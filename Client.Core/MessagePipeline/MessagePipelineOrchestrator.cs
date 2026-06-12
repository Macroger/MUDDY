// =============================================================================
/// @file       MessagePipelineOrchestrator.cs
/// @namespace  Client.Core.MessagePipeline
/// @brief      Orchestrates routing and handling of incoming server messages.
///             Validates and dispatches messages to registered handlers.
/// @details    Manages background message processing with subscription token
///             lifecycle. Implements IDisposable and must be disposed to clean up
///             subscriptions. Thread-unsafe by design — intended for single-threaded
///             use on the game loop only.
// =============================================================================

using Client.Core.Infrastructure.Events;
using Client.Core.MessagePipeline.Handlers;
using Client.Core.MessagePipeline.Routers;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;
using Shared.Network.Transport;

namespace Client.Core.MessagePipeline
{
    /// <summary>
    /// Orchestrates routing and handling of incoming messages from the server.
    /// Validates and dispatches messages to registered handlers.
    /// </summary>  
    /// 
    public class MessagePipelineOrchestrator : IDisposable
    {
        private readonly System.Collections.Concurrent.BlockingCollection<PacketEnvelope> _msgQueue = new();
        private Task? _processingTask = null;
        private CancellationTokenSource? _cts = null;
        private bool _started = false;
        
        // Used to view and publish events
        private readonly IEventBus _eventBus;
        private readonly IMessageRouter _msgRouter;
        private List<ISubscriptionToken> _subscriptions = new();

        public MessagePipelineOrchestrator(IEventBus eventBus)
        {   
            if(eventBus == null) throw new ArgumentNullException(nameof(eventBus));
            
            _eventBus = eventBus;
            _msgRouter = new BasicMessageRouter(_eventBus);
            _subscriptions = new List<ISubscriptionToken>();

            // Create list of handlers and register them to the router
            var handlers = new List<IMessageHandler>
            {
                new AuthenticationMessageHandler(_eventBus),
                new BinaryTransferMessageHandler(_eventBus),
                new ChatMessageHandler(_eventBus),
                new ErrorMessageHandler(_eventBus),
                new EventMessageHandler(_eventBus),
                new PingMessageHandler(_eventBus),
                new ResponseMessageHandler(_eventBus),
                new SystemMessageHandler(_eventBus)
            };

            // Iterate through the list and register each handler to the router
            foreach (var handler in handlers)
            {
                _msgRouter.RegisterHandler(handler.MessageType, handler);
            }

            // Attach subscription - Listen for incoming packets from the network and enqueue them for processing
            _subscriptions.Add(_eventBus.Subscribe<ClientNetworkEvents.Packets.PacketReceived>(
                EventMessageType.Network,
                OnPacketReceived));

            Start();
        }

        private void OnPacketReceived(ClientNetworkEvents.Packets.PacketReceived evnt)
        {
            // Check if background processing is running
            if(_started == false)
            {
                // log error and ignore packet
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Errors.NetworkError
                (
                    "Received packet while router is not started.",
                     null
                ));

                return;
            }

            // Enqueue the packet for processing
            if (evnt.envelope == null)
            { 
                throw new ArgumentNullException(nameof(evnt.envelope));
            }

            _msgQueue.Add(evnt.envelope);
        }     

        /// <summary>
        /// Starts the background message processing loop.
        /// </summary>
        public void Start()
        {
            // Check if already started to prevent multiple processing loops
            if (_started)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _processingTask = Task.Run(() => ProcessMessagesAsync(_cts.Token));
            _started = true;
        }

        /// <summary>
        /// Stops the background message processing loop.
        /// </summary>
        public async Task StopAsync()
        {
            // Check if already stopped to prevent unnecessary work
            if (!_started) 
            { 
                return; 
            }
            _msgQueue.CompleteAdding();
            _cts?.Cancel();
            if (_processingTask != null) await _processingTask.ConfigureAwait(false);
            _cts?.Dispose();
            _started = false;
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var envelope in _msgQueue.GetConsumingEnumerable(cancellationToken))
                {
                    try
                    {
                        await HandleMessageAsync(envelope);
                    }
                    catch (Exception ex)
                    {
                        // Log or handle handler exceptions as needed
                        System.Diagnostics.Debug.WriteLine($"Handler exception: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal on shutdown
            }
        }

        private async Task HandleMessageAsync(PacketEnvelope envelope)
        {
            var handler = _msgRouter.GetHandler(envelope);
            
            if (handler == null)
            {
                // If there is no hanlder subscribed to this message type, publish an event for the logger
                _eventBus.Publish(
                    EventMessageType.CmdPipeline,
                    new MessagePipelineEvents.Errors.MessagePipelineError(
                    $"No message handler registered for message type: {envelope.MessageType}",
                    null )
                );
            }
            else
            {
                try
                {
                    await handler.ExecuteAsync(envelope);
                }
                catch (Exception ex)
                {
                    // Log any errors that occur via publishing an event for the logger
                    _eventBus.Publish(
                        EventMessageType.CmdPipeline,
                        new MessagePipelineEvents.Errors.MessagePipelineError(ex.Message)
                    );
                }

            }            
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();

            // Dispose other resources if needed
            _cts?.Dispose();
        }
    }
}
