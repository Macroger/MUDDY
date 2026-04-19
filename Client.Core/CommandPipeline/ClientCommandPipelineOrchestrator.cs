namespace Client.Core.CommandPipeline
{
    /// <summary>
    /// Orchestrates routing and handling of incoming messages from the server.
    /// Validates and dispatches messages to registered handlers.
    /// </summary>
    /// <summary>
    /// Handles chat messages from the server. Publishes OnChatMessageReceived for GUI.
    /// </summary>
    public class ChatMessageHandler : IClientCommandHandler
    {
        public static event Action<string>? OnChatMessageReceived;

        public System.Threading.Tasks.Task HandleAsync(Shared.Protocol.Transport.TransportEnvelope envelope)
        {
            // Assume chat message is UTF-8 encoded text in payload
            var message = System.Text.Encoding.UTF8.GetString(envelope.Payload);
            OnChatMessageReceived?.Invoke(message);
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handles general event messages from the server. Publishes OnEventReceived for GUI.
    /// </summary>
    public class EventMessageHandler : IClientCommandHandler
    {
        public static event Action<string>? OnEventReceived;

        public System.Threading.Tasks.Task HandleAsync(Shared.Protocol.Transport.TransportEnvelope envelope)
        {
            // Assume event message is UTF-8 encoded text in payload
            var message = System.Text.Encoding.UTF8.GetString(envelope.Payload);
            OnEventReceived?.Invoke(message);
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handles binary transfer messages (e.g., JPEG images). Publishes OnImageReceived for GUI.
    /// </summary>
    public class BinaryTransferHandler : IClientCommandHandler
    {
        public static event Action<byte[]>? OnImageReceived;

        public System.Threading.Tasks.Task HandleAsync(Shared.Protocol.Transport.TransportEnvelope envelope)
        {
            // Pass the raw payload (expected to be JPEG image data)
            OnImageReceived?.Invoke(envelope.Payload);
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handles error messages from the server. Publishes OnErrorReceived for GUI.
    /// </summary>
    public class ErrorMessageHandler : IClientCommandHandler
    {
        public static event Action<string>? OnErrorReceived;

        public System.Threading.Tasks.Task HandleAsync(Shared.Protocol.Transport.TransportEnvelope envelope)
        {
            // Assume error message is UTF-8 encoded text in payload
            var message = System.Text.Encoding.UTF8.GetString(envelope.Payload);
            OnErrorReceived?.Invoke(message);
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handles response messages from the server. Publishes OnResponseReceived for GUI.
    /// </summary>
    public class ResponseMessageHandler : IClientCommandHandler
    {
        public static event Action<string>? OnResponseReceived;

        public System.Threading.Tasks.Task HandleAsync(Shared.Protocol.Transport.TransportEnvelope envelope)
        {
            // Assume response message is UTF-8 encoded text in payload
            var message = System.Text.Encoding.UTF8.GetString(envelope.Payload);
            OnResponseReceived?.Invoke(message);
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handles authentication success messages from the server. Publishes OnAuthSuccessReceived for GUI.
    /// </summary>
    public class AuthSuccessMessageHandler : IClientCommandHandler
    {
        public static event Action<string>? OnAuthSuccessReceived;

        public System.Threading.Tasks.Task HandleAsync(Shared.Protocol.Transport.TransportEnvelope envelope)
        {
            // Assume auth success message is UTF-8 encoded text in payload
            var message = System.Text.Encoding.UTF8.GetString(envelope.Payload);
            OnAuthSuccessReceived?.Invoke(message);
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    public class ClientCommandPipelineOrchestrator
    {
        private readonly System.Collections.Concurrent.BlockingCollection<Shared.Protocol.Transport.TransportEnvelope> _msgQueue = new();
        private readonly Dictionary<string, IClientCommandHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);
        private System.Threading.Tasks.Task? _processingTask;
        private System.Threading.CancellationTokenSource? _cts;
        private bool _started;

        /// <summary>
        /// Registers a handler for a specific message type or key.
        /// </summary>
        public bool RegisterHandler(string key, IClientCommandHandler handler)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_handlers.ContainsKey(key)) return false;
            _handlers.Add(key, handler);
            return true;
        }

        /// <summary>
        /// Enqueue a message for processing by the pipeline.
        /// </summary>
        public void ProcessMessage(Shared.Protocol.Transport.TransportEnvelope envelope)
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
            _cts = new System.Threading.CancellationTokenSource();
            _processingTask = System.Threading.Tasks.Task.Run(() => ProcessMessagesAsync(_cts.Token));
            _started = true;
        }

        /// <summary>
        /// Stops the background message processing loop.
        /// </summary>
        public async System.Threading.Tasks.Task StopAsync()
        {
            if (!_started) return;
            _msgQueue.CompleteAdding();
            _cts?.Cancel();
            if (_processingTask != null) await _processingTask.ConfigureAwait(false);
            _cts?.Dispose();
            _started = false;
        }

        private async System.Threading.Tasks.Task ProcessMessagesAsync(System.Threading.CancellationToken cancellationToken)
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

        private async System.Threading.Tasks.Task HandleMessageAsync(Shared.Protocol.Transport.TransportEnvelope envelope)
        {
            // Route by message type (or use a key from payload if needed)
            var key = envelope.MessageType.ToString();

            if (_handlers.TryGetValue(key, out var handler))
            {
                await handler.HandleAsync(envelope);
            }
        }
    }

    /// <summary>
    /// Interface for client-side command/message handlers.
    /// </summary>
    public interface IClientCommandHandler
    {
        System.Threading.Tasks.Task HandleAsync(Shared.Protocol.Transport.TransportEnvelope envelope);
    }
}
