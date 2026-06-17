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
        private bool _disposed = false;

        // Used to view and publish events
        private readonly IEventBus _eventBus = null!;
        private readonly IMessageRouter _msgRouter = null!;
        private readonly List<ISubscriptionToken> _subscriptions = new();

        public MessagePipelineOrchestrator(IEventBus eventBus)
        {   
            if(eventBus == null) throw new ArgumentNullException(nameof(eventBus));
            
            _eventBus = eventBus;
            _msgRouter = new BasicMessageRouter(_eventBus);

            // Create list of handlers and register them to the router
            var handlers = new List<IMessageHandler>
            {
                new AuthenticationMessageHandler(_eventBus),
                new BinaryTransferMessageHandler(_eventBus),
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
        }

        private void OnPacketReceived(ClientNetworkEvents.Packets.PacketReceived evnt)
        {
            // Check if background processing is running
            if(!_started)
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
            // Check if the orchestrator has been disposed to prevent starting after disposal            
            if (_disposed) throw new ObjectDisposedException(nameof(MessagePipelineOrchestrator));

            // Check if already started to prevent multiple processing loops
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
            // Check if the orchestrator has been disposed to prevent stopping after disposal
            if (_disposed) throw new ObjectDisposedException(nameof(MessagePipelineOrchestrator));

            // Check if already stopped to prevent unnecessary work
            if (!_started) return;

            // Signal the processing loop to stop and wait for it to finish
            _msgQueue.CompleteAdding();

            // Cancel the processing task if it's still running
            _cts?.Cancel();

            // Wait for the processing task to finish
            if (_processingTask != null) await _processingTask.ConfigureAwait(false);

            // Dispose of the cancellation token source
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
                        _eventBus.Publish(EventMessageType.CmdPipeline,
                            new MessagePipelineEvents.Errors.MessagePipelineError(
                             $"Exception in message processing loop: {ex.Message}", ex)
                        );
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
                // If there is no handler subscribed to this message type, publish an event for the logger
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
                        new MessagePipelineEvents.Errors.MessagePipelineError(ex.Message, ex)
                    );
                }

            }            
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                // Graceful shutdown
                StopAsync().GetAwaiter().GetResult();

                // Dispose subscriptions
                foreach (ISubscriptionToken subscription in _subscriptions)
                {
                    subscription?.Dispose();
                }
                _subscriptions.Clear();

                // Dispose handlers if they implement IDisposable
                if (_msgRouter is IDisposable disposableRouter)
                {
                    disposableRouter.Dispose();
                }

                // Dispose queue and cancellation token
                _msgQueue?.Dispose();
                _cts?.Dispose();
            }
            catch (Exception ex)
            {
                try
                {
                    // Log but don't throw from Dispose
                    _eventBus.Publish(
                        EventMessageType.CmdPipeline,
                        new MessagePipelineEvents.Errors.MessagePipelineError(
                        $"Exception during MessagePipelineOrchestrator.Dispose: {ex.Message}",
                        ex)
                    );
                }
                catch
                {
                    // Fallback: if event bus fails, use debug output
                    System.Diagnostics.Debug.WriteLine(
                        $"Exception during MessagePipelineOrchestrator.Dispose: {ex.Message}");
                }
            }

            _disposed = true;
        }
    }
}
