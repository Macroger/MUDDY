using Server.Core.Infrastructure.Identity.MessageId;
using Server.Core.Infrastructure.Lifecycle;
using Server.Core.Network.Supervisor;
using Shared.EventBus;
using Shared.Identity;
using Shared.Protocol.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Server.Core.CommandPipeline
{
    public sealed class CommandPipelineOrchestrator : IStartable, IStoppable
    {
        private readonly BlockingCollection<TransportEnvelope> _msgEnvelopeQueue = new BlockingCollection<TransportEnvelope>();

        #region Dependencies

        private readonly IEventBus _eventBus;
        private readonly INetworkSupervisor _networkSupervisor;
        private readonly IMessageIdGenerator _messageIdGenerator;
        private readonly ICommandParser _cmdParser;
        private readonly ICommandRouter _cmdRouter;
        private readonly ICommandContextBuilder _contextBuilder;
        private readonly List<ICommandPolicy> _preParsePolicies;
        private readonly List<ICommandPolicy> _postParsePolicies;

        #endregion

        private Task? _msgProcessingTask;
        private CancellationTokenSource? _cts;

        public CommandPipelineOrchestrator(
        IEventBus eventBus,
        INetworkSupervisor networkSupervisor,
        ICommandRouter router,
        ICommandParser parser,
        IMessageIdGenerator messageIdGenerator,
        ICommandContextBuilder contextBuilder,
        IEnumerable<ICommandPolicy> preParsePolicy,
        IEnumerable<ICommandPolicy> postParsePolicy)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _cmdRouter = router ?? throw new ArgumentNullException(nameof(router));
            _cmdParser = parser ?? throw new ArgumentNullException(nameof(parser));
            _messageIdGenerator = messageIdGenerator ?? throw new ArgumentNullException(nameof(messageIdGenerator));
            _networkSupervisor = networkSupervisor ?? throw new ArgumentNullException(nameof(_networkSupervisor));
            _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
            _preParsePolicies = preParsePolicy?.ToList() ?? throw new ArgumentNullException(nameof(preParsePolicy));
            _postParsePolicies = postParsePolicy?.ToList() ?? throw new ArgumentNullException(nameof(postParsePolicy));
        }

        /// <summary>
        /// Adds the specified transport envelope to the processing queue.
        /// </summary>
        /// <param name="envelope">The transport envelope to be queued for processing. Cannot be null.</param>
        public void ProcessMessage(TransportEnvelope envelope)
        {
            // Basic validation - check if null
            if (envelope == null) throw new ArgumentNullException(nameof(envelope), "Transport envelope cannot be null");

            _msgEnvelopeQueue.Add(envelope);
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _msgProcessingTask = ProcessMessagesAsync(_cts.Token);
        }

        public async Task StopAsync()
        {
            _msgEnvelopeQueue.CompleteAdding(); // Signals that we want to stop adding items
            _cts?.Cancel();                     // Signal that we want to cancel all other activities

            // If the processing task is still running, wait for it to finish. It will complete once all messages are processed or cancellation is observed.
            if ( _msgProcessingTask != null )  await _msgProcessingTask.ConfigureAwait(false);

            _cts?.Dispose();                     // Dispose of the cancellation token source
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var envelope in _msgEnvelopeQueue.GetConsumingEnumerable(cancellationToken))
                {
                    try
                    {
                        await HandleMessageAsync(envelope);
                    }
                    catch (Exception ex)
                    {
                        EventBusHelper.PublishEvent(
                            _eventBus,
                            EventMessageType.Error,
                            new EventReason(
                                "Exception during message handling",
                                new { exception = ex.Message, envelope.MessageId }
                            )
                        );
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected exception when cancellation is requested. We can ignore this.
            }
            catch (Exception ex)
            {
                // Log or handle unexpected exceptions that occur during message processing.
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason(
                        "Unexpected exception in message processing loop",
                        new { exception = ex.Message }
                    )
                );
            }
        }

        private async Task HandleMessageAsync(TransportEnvelope msg)
        {
            // Validate that the msg is not null.
            if (msg == null)
            {
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason(
                        "Received null message envelope",
                        null
                    )
                );
                return;
            }
            
            // 1st pass Policy check
            foreach (var policy in _preParsePolicies)
            {
                var result = policy.Check(msg);
                if (!result.IsValid)
                {
                    // Log the policy failure event
                    EventBusHelper.PublishEvent(
                        _eventBus,
                        EventMessageType.Error,
                        new EventReason(
                            "Pre-parse policy check failed",
                            new { policy = policy.GetType().Name, messageId = msg.MessageId }
                        )
                    );

                    TransportEnvelope response = CreateErrorResponse(
                        errorType: TransportMessageType.Error, 
                        message: result.ErrorMessage ?? "Unknown parsing error",
                        connId: msg.connId);

                    _networkSupervisor.SendToClient(msg.connId, response);

                    // As soon as any policy fails, we stop processing this message.
                    return;
                }
            }

            // Parse
            ParseResult parseResult = _cmdParser.Parse(msg);
            if (!parseResult.Success)
            {
                TransportEnvelope response = CreateErrorResponse(
                        errorType: TransportMessageType.Error,
                        message: parseResult.ErrorMessage ?? "Unknown parsing error",
                        connId: msg.ConnId);

                _networkSupervisor.SendToClient(msg.ConnId, response);
                return;
            }

            // Build context (player state, inventory, effects, etc.)
            var context = await _contextBuilder.BuildAsync(parseResult.Command);
            if (context == null)
            {
                _networkSupervisor.SendToClient(msg.ConnId, PlayerNotFoundResponse);
                return;
            }

            // 2nd Pass: Dynamic policy (player-state level)
            foreach (var policy in _postParsePolicy)
            {
                var result = policy.Check(context);  // <-- Has access to player state AND command type
                if (!result.IsValid)
                {
                    _networkSupervisor.SendToClient(msg.ConnId, result.ErrorResponse);
                    return;
                }
            }

            // Route & Execute
            var handler = _cmdRouter.Route(context.Command);
            if (handler == null)
            {
                _networkSupervisor.SendToClient(msg.ConnId, CommandNotFoundResponse);
                return;
            }

            var commandResult = await handler.ExecuteAsync(context);
            _networkSupervisor.SendToClient(msg.ConnId, commandResult);
        }


        private TransportEnvelope CreateErrorResponse(TransportMessageType? errorType, string? message, ConnectionId connId, MessageId? msgId = null)
        {
            TransportEnvelope response = new TransportEnvelope(
                    messageId: _messageIdGenerator.New(),
                    messageCorrelationId: msgId,
                    messageType: errorType ?? TransportMessageType.Error,
                    flags: Shared.Protocol.Types.MessageFlags.None,
                    payload: Encoding.UTF8.GetBytes(message ?? "Unknown parsing error"),
                    connectionId: connId
                );

            return response;
        }
    }
}
