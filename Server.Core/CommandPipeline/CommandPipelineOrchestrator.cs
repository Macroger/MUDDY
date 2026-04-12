using Server.Core.CommandPipeline.Authentication;
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Parser;
using Server.Core.CommandPipeline.Policies;
using Server.Core.CommandPipeline.Types;
using Server.Core.Infrastructure.Identity.MessageId;
using Server.Core.Infrastructure.Lifecycle;
using Server.Core.Network.Supervisor;
using Shared.EventBus;
using Shared.Identity;
using Shared.Protocol.Transport;
using System.Collections.Concurrent;
using System.Text;

namespace Server.Core.CommandPipeline
{
    public sealed class CommandPipelineOrchestrator : IStartable, IStoppable
    {
        private readonly BlockingCollection<TransportEnvelope> _msgEnvelopeQueue = new BlockingCollection<TransportEnvelope>();

        private bool _started;
        #region Dependencies

        private readonly IEventBus _eventBus;
        private readonly INetworkSupervisor _networkSupervisor;
        private readonly IMessageIdGenerator _messageIdGenerator;
        private readonly ICommandParser _cmdParser;
        private readonly ICommandRouter _cmdRouter;
        private readonly ICommandContextBuilder _contextBuilder;
        private readonly IAuthenticationPipeline _authenticationPipeline;
        private readonly List<IFirstPassPolicy> _firstPassPolicies;
        private readonly List<ISecondPassPolicy> _secondPassPolicies;

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
        IAuthenticationPipeline authenticationPipeline,
        IEnumerable<IFirstPassPolicy> firstPassPolicies,
        IEnumerable<ISecondPassPolicy> secondPassPolicies)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _cmdRouter = router ?? throw new ArgumentNullException(nameof(router));
            _cmdParser = parser ?? throw new ArgumentNullException(nameof(parser));
            _messageIdGenerator = messageIdGenerator ?? throw new ArgumentNullException(nameof(messageIdGenerator));
            _networkSupervisor = networkSupervisor ?? throw new ArgumentNullException(nameof(_networkSupervisor));
            _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
            _authenticationPipeline = authenticationPipeline ?? throw new ArgumentNullException(nameof(authenticationPipeline));
            _firstPassPolicies = firstPassPolicies?.ToList() ?? throw new ArgumentNullException(nameof(firstPassPolicies));
            _secondPassPolicies = secondPassPolicies?.ToList() ?? throw new ArgumentNullException(nameof(secondPassPolicies));
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

       /// <summary>
       /// Starts the message processing operation if it has not already been started.
       /// </summary>
       /// <remarks>Subsequent calls to this method have no effect if the operation is already running. Use
       /// this method to initiate background message processing.</remarks>
        public void Start()
        {
            if(_started) return; // Prevent multiple starts

            _cts = new CancellationTokenSource();
            _msgProcessingTask = ProcessMessagesAsync(_cts.Token);
            _started = true;
        }

        /// <summary>
        /// Stops the current operation if it has been started.
        /// </summary>
        /// <remarks>If the operation has not been started, this method performs no action. This method is
        /// asynchronous and returns immediately; any ongoing stop process continues in the background.</remarks>
        public async void Stop()
        {
            if(!_started) return; // If not started, nothing to stop

            await StopAsync();

            _started = false;
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
            
            // 1st pass Policy check - AuthenticationPolicy
            foreach (var policy in _firstPassPolicies)
            {
                PolicyResult result = await policy.CheckPolicyAsync(msg);
                if (!result.IsValid)
                {
                    // Log the policy failure event
                    EventBusHelper.PublishEvent(
                        _eventBus,
                        EventMessageType.Error,
                        new EventReason(
                            result.ErrorMessage ?? "Authentication failed",
                            new { policy = policy.GetType().Name, messageId = msg.MessageId }
                        )
                    );

                    TransportEnvelope response = CreateErrorResponse(
                        errorType: TransportMessageType.Error, 
                        message: result.ErrorMessage ?? "Authentication failed",
                        connId: msg.ConnId);

                    _networkSupervisor.SendToClient(msg.ConnId, response);

                    // As soon as any policy fails, we stop processing this message.
                    return;
                }
            }

            // Check if unauthenticated - route to auth pipeline instead of main pipeline
            if (msg.SessionToken == SessionId.Unauthenticated)
            {
                await _authenticationPipeline.ProcessAuthCommandAsync(msg);
                return;  // Don't continue to main pipeline
            }
            // Player is authenticated - continue with main pipeline

            // Parse
            ParseResult parseResult = _cmdParser.Parse(msg);
            if (!parseResult.Success)
            {
                TransportEnvelope errorResponseEnvelope = new TransportEnvelope(
                    messageId: _messageIdGenerator.New(),
                    sessionId: null,
                    messageCorrelationId: msg.MessageId,
                    messageType: TransportMessageType.Error,
                    flags: Shared.Protocol.Types.MessageFlags.None,
                    payload: Encoding.UTF8.GetBytes(parseResult.ErrorMessage ?? "Unknown authentication error."),
                    connectionId: msg.ConnId
                );

                _networkSupervisor.SendToClient(msg.ConnId, errorResponseEnvelope);
                return;
            }

            // Build context (player state, inventory, effects, etc.)
            CommandContext context = await _contextBuilder.BuildContextAsync(msg.ConnId, parseResult.Command!);
            if (!context.Success)
            {
                TransportEnvelope errorResponseEnvelope = new TransportEnvelope(
                    messageId: _messageIdGenerator.New(),
                    sessionId: null,
                    messageCorrelationId: msg.MessageId,
                    messageType: TransportMessageType.Error,
                    flags: Shared.Protocol.Types.MessageFlags.None,
                    payload: Encoding.UTF8.GetBytes(context.ErrorMessage ?? "Unknown parsing error."),
                    connectionId: msg.ConnId
                );
                _networkSupervisor.SendToClient(msg.ConnId, errorResponseEnvelope);
                return;
            }

            // 2nd Pass: Dynamic policy (player-state level)
            foreach (var policy in _secondPassPolicies)
            {
                var result = await policy.CheckPolicyAsync(context); 
                if (!result.IsValid)
                {
                    TransportEnvelope errorResponseEnvelope = new TransportEnvelope(
                    messageId: _messageIdGenerator.New(),
                    sessionId: null,
                    messageCorrelationId: msg.MessageId,
                    messageType: TransportMessageType.Error,
                    flags: Shared.Protocol.Types.MessageFlags.None,
                    payload: Encoding.UTF8.GetBytes(result.ErrorMessage ?? "Unknown policy error."),
                    connectionId: msg.ConnId
                    );
                    _networkSupervisor.SendToClient(msg.ConnId, errorResponseEnvelope);
                    return;
                }
            }

            // Route & Execute
            var handler = _cmdRouter.Route(context.Command);
            if (handler == null)
            {
                TransportEnvelope errorResponseEnvelope = new TransportEnvelope(
                    messageId: _messageIdGenerator.New(),
                    sessionId: null,
                    messageCorrelationId: msg.MessageId,
                    messageType: TransportMessageType.Error,
                    flags: Shared.Protocol.Types.MessageFlags.None,
                    payload: Encoding.UTF8.GetBytes(context.ErrorMessage ?? "Unknown routing error."),
                    connectionId: msg.ConnId
                    );
                _networkSupervisor.SendToClient(msg.ConnId, errorResponseEnvelope);
                return;
            }

            var commandResult = await handler.ExecuteAsync(context);
            _networkSupervisor.SendToClient(msg.ConnId, commandResult);
        }


        private TransportEnvelope CreateErrorResponse(TransportMessageType? errorType, string? message, ConnectionId connId, MessageId? msgId = null)
        {
            TransportEnvelope response = new TransportEnvelope(
                    messageId: _messageIdGenerator.New(),
                    sessionId: null,
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
