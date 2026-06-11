using Client.Core.Infrastructure.Events;
using Client.Core.MessagePipeline.Handlers;
using Client.Core.MessagePipeline.Routers;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;
using Shared.Network.Transport;
using Shared.Network.Types;

namespace Client.Core.MessagePipeline
{
    /// <summary>
    /// Orchestrates routing and handling of incoming messages from the server.
    /// Validates and dispatches messages to registered handlers.
    /// </summary>  
    /// 
    public class MessagePipelineOrchestrator : IMessageRouter
    {
        private readonly System.Collections.Concurrent.BlockingCollection<PacketEnvelope> _msgQueue = new();
        private readonly Dictionary<PacketType, IMessageHandler> _handlers;
        private Task? _processingTask;
        private CancellationTokenSource? _cts;
        private bool _started;
        
        // Used to view and publish events
        private readonly IEventBus _eventBus;

        private ISubscriptionToken _incommingPacketSubscription;

        public MessagePipelineOrchestrator(IEventBus eventBus)
        {   
            _eventBus = eventBus;
            _handlers = new Dictionary<PacketType, IMessageHandler>();

            // Attach subscription
            _incommingPacketSubscription = _eventBus.Subscribe<ClientNetworkEvents.Packets.PacketReceived>(
                EventMessageType.Network,
                OnPacketReceived);

            Start();
        }

        private void OnPacketReceived(ClientNetworkEvents.Packets.PacketReceived evt)
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
            Route(evt.envelope);
        }

        /// <summary>
        /// Registers a handler for a specific message type or key.
        /// </summary>
        public bool RegisterHandler(PacketType key, IMessageHandler handler)
        {
            // Validate inputs
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_handlers.ContainsKey(key) == true) return false;

            // Add handler to dictionary
            _handlers.Add(key, handler);
            return true;
        }

        /// <summary>
        /// Enqueue a message for processing by the pipeline.
        /// </summary>
        public void Route(PacketEnvelope envelope)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            
            _msgQueue.Add(envelope);
        }

        /// <summary>
        /// Starts the background message processing loop.
        /// </summary>
        public void Start()
        {
            if (_started) return;
            _cts = new CancellationTokenSource();
            _processingTask = Task.Run(() => ProcessMessagesAsync(_cts.Token));
            _started = true;
        }

        /// <summary>
        /// Stops the background message processing loop.
        /// </summary>
        public async Task StopAsync()
        {
            if (!_started) return;
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
            // Route by message type
            var key = envelope.MessageType;

            if (_handlers.TryGetValue(key, out var handler))
            {
                await handler.ExecuteAsync(envelope);
            }            
            else
            {
                // Incase there is no hanlder subscribed to this message type, publish an event for the logger
                _eventBus.Publish(
                    EventMessageType.CmdPipeline,
                    new MessageRouterEvents.Errors.MessageRouterError
                (
                    $"No message handler registered for message type: {key}",
                     null
                ));
            }
        }
    }
}
