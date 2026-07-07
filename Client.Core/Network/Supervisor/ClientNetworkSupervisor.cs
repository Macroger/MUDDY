// =============================================================================
/// @file       ClientNetworkSupervisor.cs
/// @namespace  Client.Core.Network.Supervisor
/// @brief      Manages client-to-server connection lifecycle and translation layer.
///             Owns single IClientConnectionWorker and translates infrastructure
///             events to domain events via event bus.
/// @details    Supervisor acts as the boundary between infrastructure (C# events)
///             and domain (event bus). All public API routes through event bus;
///             internal worker communication uses direct C# events.
// =============================================================================

using Client.Core.Infrastructure.Events;
using Client.Core.Network.Worker;
using Client.Core.Services.Authentication;
using Client.Core.Services.Command;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;
using Shared.Network.Transport;
using Shared.Network.Types;
using System.Text;

namespace Client.Core.Network.Supervisor
{
    /// <summary>
    /// Manages the lifecycle of the client-to-server connection and acts as a translation layer
    /// </summary>
    public sealed class ClientNetworkSupervisor : IClientNetworkSupervisor
    {
        #region Private Fields

        private readonly IEventBus _eventBus = null!;
        private readonly IAuthenticationService _authService = null!;
        private readonly List<ISubscriptionToken> _subscriptions = new();
        private readonly ICommandSerializer _commandSerializer = null!;

        private IClientConnectionWorker? _worker = null;
        private CancellationTokenSource _supervisorCts = null!;
        private CancellationTokenSource? _connectionCts = null;

        private readonly object _connectionLock = new object();

        private bool _isConnected = false;
        private bool _disposed = false;
        private bool _isShuttingDown = false;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets a value indicating whether the supervisor is connected to the server.
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            private set => _isConnected = value;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ClientNetworkSupervisor.
        /// </summary>
        /// <param name="eventBus">The event bus for publishing domain events.</param>
        /// <exception cref="ArgumentNullException">Thrown if eventBus is null.</exception>
        public ClientNetworkSupervisor(IEventBus eventBus, ICommandSerializer commandSerializer)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _supervisorCts = new CancellationTokenSource();
            _authService = new AuthenticationService(_eventBus);
            _commandSerializer = commandSerializer ?? throw new ArgumentNullException(nameof(commandSerializer));

            // Subscribe to event bus commands
            SubscribeToEventBusCommands();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Establishes a connection to the server at the specified address and port.
        /// </summary>
        /// <remarks>
        /// Uses a lock to ensure only one connection attempt can proceed at a time.
        /// Concurrent calls will be rejected while a connection is being established.
        /// The lock is released immediately after creating and wiring the worker;
        /// the actual socket connection happens on the worker's background threads.
        /// </remarks>
        /// <param name="serverAddress">The server address.</param>
        /// <param name="serverPort">The server port.</param>
        /// <returns>True if connection was established; otherwise, false.</returns>
        public async Task<bool> StartConnectionAsync(string serverAddress, int serverPort)
        {
            _isShuttingDown = false;

            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Errors.NetworkError(
                        "Cannot connect: server address is null or empty."));
                return false;
            }

            if (serverPort <= 0 || serverPort > 65535)
            {
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Errors.NetworkError(
                        $"Cannot connect: invalid port number {serverPort}."));
                return false;
            }

            IClientConnectionWorker? newWorker = null;

            try
            {
                lock (_connectionLock)
                {
                    if (IsConnected || _worker != null)
                    {
                        _eventBus.Publish(
                            EventMessageType.Network,
                            new ClientNetworkEvents.Errors.NetworkError(
                                "Cannot connect: already connected to server or connection attempt in progress."));
                        return false;
                    }

                    _connectionCts?.Dispose();
                    _connectionCts = CancellationTokenSource.CreateLinkedTokenSource(_supervisorCts.Token);

                    newWorker = new ClientConnectionWorker(
                        serverAddress: serverAddress,
                        serverPort: serverPort,
                        packetFactory: new MuddyPacketFactory(),
                        envelopeFactory: new MessageEnvelopeFactory(),
                        packetSerializer: new MuddyPacketSerializer(new MuddyProtocolLimits()),
                        protocolLimits: new MuddyProtocolLimits(),
                        cancellationToken: _connectionCts.Token);

                    newWorker.PacketReceived += OnWorkerPacketReceived;
                    newWorker.PacketSent += OnWorkerPacketSent;
                    newWorker.ConnectionClosed += OnWorkerConnectionClosed;
                    newWorker.ErrorOccurred += OnWorkerErrorOccurred;
                }

                bool startResult = newWorker.Start();

                if (!startResult)
                {
                    _eventBus.Publish(
                        EventMessageType.Network,
                        new ClientNetworkEvents.Errors.NetworkError(
                            $"Failed to start connection worker for {serverAddress}:{serverPort}."));
                    return false;
                }

                lock (_connectionLock)
                {
                    _worker = newWorker;
                    IsConnected = true;
                }

                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Lifecycle.ConnectionStatusChangedEvent(
                        ConnectionStatus: true,
                        Message: $"Connected to {serverAddress}:{serverPort}"));

                return true;
            }
            catch (Exception ex)
            {

                lock (_connectionLock)
                {
                    IsConnected = false;
                }

                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Errors.NetworkError(
                        $"Failed to establish connection to {serverAddress}:{serverPort}: {ex.GetType().Name}: {ex.Message}",
                        ex));

                return false;
            }
        }

        /// <summary>
        /// Closes the connection to the server.
        /// </summary>
        /// <returns>True if disconnection was successful; otherwise, false.</returns>
        public bool StopWorker()
        {
            try
            {
                lock (_connectionLock)
                {
                    if (!IsConnected || _worker == null)
                    {
                        return true;
                    }

                    _isShuttingDown = true;
                    _worker.Stop();
                    _worker = null;
                    IsConnected = false;
                }

                // Publish domain event
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Lifecycle.ConnectionStatusChangedEvent(
                        ConnectionStatus: IsConnected,
                        Message: "Disconnected from server"));

                return true;
            }
            catch (Exception ex)
            {
                IsConnected = false;

                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Errors.NetworkError(
                        $"Error while disconnecting from server: {ex.Message}", ex));

                return false;
            }
        }

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="envelope">The transport envelope to send.</param>
        /// <returns>True if message was sent successfully; otherwise, false.</returns>
        public bool SendToServer(MessageEnvelope envelope)
        {
            try
            {
                if (!IsConnected || _worker == null)
                {
                    _eventBus.Publish(
                        EventMessageType.Network,
                        new ClientNetworkEvents.Errors.NetworkError(
                            $"Cannot send message: not connected to server. PacketType: {envelope.MessageType}"));
                    return false;
                }

                bool sendResult = _worker.SendEnvelope(envelope);

                if (!sendResult)
                {
                    _eventBus.Publish(
                        EventMessageType.Network,
                        new ClientNetworkEvents.Errors.NetworkError(
                            $"Failed to queue message for sending. Message Type: {envelope.MessageType} (ID: {envelope.MessageId})"));
                }

                return sendResult;
            }
            catch (Exception ex)
            {
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Errors.NetworkError(
                        $"Exception while sending message to server: {ex.Message}", ex));

                return false;
            }
        }

        /// <summary>
        /// Performs graceful shutdown of the supervisor and all resources.
        /// </summary>
        public void ShutdownSupervisor()
        {
            try
            {
                StopWorker();
                _supervisorCts.Cancel();

                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Lifecycle.SupervisorShutdown(
                        "Client network supervisor shutting down."));
            }
            catch (Exception ex)
            {
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Errors.NetworkError(
                        $"Exception during supervisor shutdown: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// Subscribes to relevant commands on the event bus - such as requests to connect to the server.
        /// </summary>
        private void SubscribeToEventBusCommands()
        {
            // Subscribe to and listen for outbound message events
            _subscriptions.Add(_eventBus.Subscribe<ClientNetworkEvents.Commands.ConnectToServer>(
                eventType: EventMessageType.Network,
                handler: OnConnectToServerRequest
            ));

            // Subscribe to and listen for outbound message events
            _subscriptions.Add(_eventBus.Subscribe<ClientNetworkEvents.Commands.DisconnectFromServer>(
                eventType: EventMessageType.Network,
                handler: OnDisconnectFromServerRequest
            ));

            // Subscribe to and listen for outbound message events
            _subscriptions.Add(_eventBus.Subscribe<ClientGuiEvents.Commands.SendMessageToServer>(
                eventType: EventMessageType.Gui,
                handler: OnSendMessageRequest
            ));

            // Subscribe to and listen for ping requests
            _subscriptions.Add(_eventBus.Subscribe<ClientNetworkEvents.Commands.SendPingToServer>(
                eventType: EventMessageType.Network,
                handler: OnSendPingRequest
            ));
        }

        private MessageEnvelope? ConvertMessageToPacketEnvelope(string message)
        {
            if (message == null) 
            {
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Errors.SerializerError("Unable convert message into packet; message is null."));

                return null;
            }

            try
            {
                // Use the command serializer service to convert text to JSON
                string json = _commandSerializer.SerializeCommand(message);

                // Generate a packet envelope from the JSON
                MessageEnvelope envelope = new(
                    sessionId: _authService.SessionId,
                    messageType: PacketType.Command,
                    payload: Encoding.UTF8.GetBytes(json)
                );

                return envelope;
            }
            catch (Exception ex)
            {
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Errors.SerializerError(
                        $"Failed to serialize command: {ex.Message}"));
                return null;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Emits an event on the event bus for the received packet from the worker.
        /// </summary>
        private void OnWorkerPacketReceived(object? sender, MessageEnvelope envelope)
        {
            try
            {
                // Emit an event on the event bus for the received packet
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Packets.PacketReceived(envelope));
            }
            catch (Exception ex)
            {
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Errors.NetworkError(
                        $"Exception processing received packet: {ex.Message}", ex));
            }
        }
        
        /// <summary>
        /// Emits an event on the event bus when the worker signals that a packet has been sent to the server.
        /// </summary>
        private void OnWorkerPacketSent(object? sender, MessageEnvelope envelope)
        {
            try
            {
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Packets.PacketSent(envelope));
            }
            catch (Exception ex)
            {
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.Errors.NetworkError(
                        $"Exception publishing PacketSent event: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// Emits an event on the event bus when the worker signals that the connection has been closed.
        /// </summary>
        private void OnWorkerConnectionClosed(object? sender, EventArgs e)
        {
            IsConnected = false;

            StopWorker();

            _eventBus.Publish(
                EventMessageType.Network,
                new ClientNetworkEvents.Lifecycle.ConnectionStatusChangedEvent(
                    ConnectionStatus: IsConnected,
                    Message: "Connection to server closed by worker."));
        }

        /// <summary>
        /// Emits an event on the event bus when the worker signals that an error has occurred.
        /// </summary>
        private void OnWorkerErrorOccurred(object? sender, Exception ex)
        {
            // During intentional shutdown, I/O cancellation errors are expected - suppress them
            if (_isShuttingDown && IsExpectedShutdownException(ex))
            {
                return;
            }

            _eventBus.Publish(
                EventMessageType.Network,
                new ClientNetworkEvents.Errors.NetworkError(
                    $"Error in connection worker: {ex.Message}", ex));
        }

        /// <summary>
        /// Handles the ConnectToServer command from the event bus.
        /// Dispatched onto a thread pool thread immediately so the event bus
        /// caller's thread (typically the UI thread) is never blocked by the
        /// TCP connect handshake.
        /// </summary>
        private void OnConnectToServerRequest(ClientNetworkEvents.Commands.ConnectToServer evnt)
        {
            Task.Run(() => StartConnectionAsync(evnt.serverAddress, evnt.serverPort))
                .ContinueWith(
                    task => _eventBus.Publish(
                        EventMessageType.Network,
                        new ClientNetworkEvents.Errors.NetworkError(
                            $"Unhandled exception during connect: {task.Exception!.InnerException?.Message ?? task.Exception.Message}",
                            task.Exception.InnerException ?? task.Exception)),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Handles the DisconnectFromServer command from the event bus.
        /// Dispatched onto a thread pool thread so that socket teardown
        /// does not block the event bus caller's thread.
        /// </summary>
        private void OnDisconnectFromServerRequest(ClientNetworkEvents.Commands.DisconnectFromServer evnt)
        {
            Task.Run(() => StopWorker())
                .ContinueWith(
                    task => _eventBus.Publish(
                        EventMessageType.Network,
                        new ClientNetworkEvents.Errors.NetworkError(
                            $"Unhandled exception during disconnect: {task.Exception!.InnerException?.Message ?? task.Exception.Message}",
                            task.Exception.InnerException ?? task.Exception)),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Handles the SendMessageToServer command from the event bus, converts the message to a MessageEnvelope,
        /// and sends it to the server via the worker.
        /// </summary>
        /// <param name="evnt">The event containing the message to send to the server.</param>
        private void OnSendMessageRequest(ClientGuiEvents.Commands.SendMessageToServer evnt)
        {
            // Convert the message to a packet envelope
            var pktEnvelope = ConvertMessageToPacketEnvelope(evnt.Message);

            // If conversion failed, return early (error will be logged inside ConvertMessageToPacketEnvelope)
            if (pktEnvelope == null) return;

            SendToServer(pktEnvelope);
        }

        /// <summary>
        /// Handles the SendPingToServer command from the event bus, creates a ping MessageEnvelope,
        /// and sends it to the server via the worker.
        /// </summary>
        /// <param name="evnt">The event indicating that a ping should be sent to the server.</param>
        private void OnSendPingRequest(ClientNetworkEvents.Commands.SendPingToServer evnt)
        {
            // Create a ping packet envelope
            MessageEnvelope pingEnvelope = new(
                sessionId: _authService.SessionId,
                messageType: PacketType.Ping,
                payload: Array.Empty<byte>());

            SendToServer(pingEnvelope);
        }

        #endregion

        #region Helper Methods

        private bool IsExpectedShutdownException(Exception ex)
        {
            // IOException with "aborted" occurs when stream operations are cancelled
            if (ex is IOException ioEx
                && ioEx.Message.Contains("aborted", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // ObjectDisposedException can occur if stream is disposed during read/write
            if (ex is ObjectDisposedException)
            {
                return true;
            }

            // OperationCanceledException is already handled in the worker, but just in case
            if (ex is OperationCanceledException)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Disposal

        /// <summary>
        /// Disposes the supervisor and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                ShutdownSupervisor();

                // Dispose subscriptions
                foreach (ISubscriptionToken token in _subscriptions)
                {
                    token?.Dispose();
                }
                _subscriptions.Clear();

                // Dispose cancellation token source
                _supervisorCts?.Dispose();
                _connectionCts?.Dispose();

                if (_worker is IDisposable disposableWorker)
                {
                    disposableWorker.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Exception during ClientNetworkSupervisor.Dispose: {ex.Message}");
            }

            _disposed = true;
        }

        #endregion
    }
}