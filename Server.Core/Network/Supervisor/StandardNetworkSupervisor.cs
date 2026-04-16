using Server.Core.CommandPipeline;
using Server.Core.Infrastructure.Identity.ConnectionId;
using Server.Core.Infrastructure.Identity.MessageId;
using Server.Core.Infrastructure.Lifecycle;
using Server.Core.Network.Listener;
using Server.Core.Network.Model;
using Server.Core.Network.Worker;
using Shared.EventBus;
using Shared.EventBus.DomainEvents;
using Shared.EventBus.SubscriptionToken;
using Shared.Identity;
using Shared.Protocol.System;
using Shared.Protocol.Transport;
using Shared.Protocol.Types;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Windows.Media.Protection.PlayReady;
using static Shared.EventBus.DomainEvents.NetworkEvents;


namespace Server.Core.Network.Supervisor
{
    public class StandardNetworkSupervisor : 
        IDisposable,
        INetworkSupervisor,
        IListenerErrorHandler,
        IConnectionAcceptedHandler,
        IStartable,
        IStoppable
    {
        #region Dependencies
        private IConnectionIdGenerator _connectionIdGenerator;        
        private IEventBus _eventBus;
        private CommandPipelineOrchestrator? _commandPipeline;
        private TcpConnectionListener _tcpConnectionListener;
        private MuddyPacketFactory _packetFactory;
        private MuddyProtocolLimits _packetLimits;
        private MuddyPacketSerializer _packetSerializer;
        private CancellationTokenSource _serverCts;
        private IPEndPoint _listenerEndPoint;
        private readonly IServerLifecycle _lifecycle;
        private List<ISubscriptionToken> _subscriptions = new List<ISubscriptionToken>();
        private IMessageIdGenerator _messageIdGenerator;
        #endregion

        private ConcurrentDictionary<ConnectionId, ConnectionContext> _activeConnections = new ConcurrentDictionary<ConnectionId, ConnectionContext>();

        public bool IsListeningForConnections { get; private set; } = false;

        /// <summary>
        /// Constructs a new <see cref="StandardNetworkSupervisor"/>.
        /// </summary>
        /// <param name="tcpListener">A TCP connection listener instance (not used directly in current implementation but reserved for DI scenarios).</param>
        /// <param name="commandPipeline">Command pipeline orchestrator used to process incoming messages.</param>
        /// <param name="bus">The event bus used for publishing network and error events.</param>
        /// <param name="port">The port number on which the supervisor should listen for incoming connections. Must be in the range 1..65535.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the provided <paramref name="port"/> is outside the valid range.</exception>
        public StandardNetworkSupervisor( 
            IServerLifecycle lifeCycle,
            IEventBus bus, 
            IMessageIdGenerator messageIdGenerator,
            int port = 30333)
        {
            // Validate port number is acceptable.
            if(port <= 1 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port), "Port number must be between 1 and 65535.");

            // Dependencies - Generate new instances for use internally
            _connectionIdGenerator = new ConnectionIdGenerator();
            _messageIdGenerator = messageIdGenerator;
            _listenerEndPoint = new IPEndPoint(IPAddress.Any, port);
            _tcpConnectionListener = new TcpConnectionListener(
               localEndPoint: _listenerEndPoint,
               supervisor: this,
               connIdGenerator: _connectionIdGenerator,
               listenerErrorHandler: this,
               connectionAcceptedHandler: this);
            _serverCts = new CancellationTokenSource();
            _packetFactory = new MuddyPacketFactory();
            _packetLimits = new MuddyProtocolLimits();
            _packetSerializer = new MuddyPacketSerializer(_packetLimits);

            // Wire up DI items
            _commandPipeline = null;
            _eventBus = bus;
            _lifecycle = lifeCycle;
        }

        /// <summary>
        /// Broadcasts a protocol message to all active client connections.
        /// </summary>
        /// <param name="msg">The protocol envelope to broadcast to all clients.</param>
        /// <remarks>Publishes a network event indicating a broadcast is taking place and iterates through all active connections, sending the provided message to each client's connection worker. If sending to a particular client fails, an error event is published and the broadcast continues for remaining clients.</remarks>
        public void BroadcastMessage(TransportEnvelope msg)
        {
            EventBusHelper.PublishEvent(
                _eventBus,
                EventMessageType.Network,
                new EventReason($"Broadcasting message to all active connections: {msg.MessageType} (ID: {msg.MessageId}, {_activeConnections.Count} connections)")
            );

            // Loop through the active connections and send the message to each client using the connection worker.
            foreach (var connection in _activeConnections.Values)
            {
                try
                {
                    connection.Worker.SendMessage(msg);
                }
                catch
                {
                    EventBusHelper.PublishEvent(
                        _eventBus,
                        EventMessageType.Error,
                        new EventReason($"Failed to send broadcast message to client {connection.ClientConnection.connId}: {msg.MessageType} (ID: {msg.MessageId})")
                    );
                    continue;
                }
            }
        }

        /// <summary>
        /// Marks a specific connection for closure and attempts to terminate it.
        /// </summary>
        /// <param name="connectionId">The identifier of the connection to close.</param>
        /// <param name="reason">The reason for closing the connection.</param>
        /// <remarks>Attempts to locate the connection context for the given connection ID. If found, the connection's cancellation token is requested and its worker is stopped. A network event is published for both success and the case where the connection was not found. Any exceptions during the process are published as error events.</remarks>
        public void CloseConnection(ConnectionId connectionId, ConnectionCloseReason reason)
        {
            try
            {
                if (_activeConnections.TryGetValue(connectionId, out var context))
                {
                    context.CancellationSource.Cancel();
                    context.Worker.Stop();

                    EventBusHelper.PublishEvent(
                        _eventBus,
                        EventMessageType.Network,
                        new EventReason($"Connection marked for closure: {connectionId} (Reason: {reason})"));
                }
                else
                {
                    EventBusHelper.PublishEvent(
                        _eventBus,
                        EventMessageType.Network,
                        new EventReason($"Connection not found, cannot close connection: {connectionId} (Reason: {reason})"));
                }
            }
            catch (Exception ex)
            {
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason($"Exception while closing connection {connectionId} (Reason: {reason}): {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Processes a newly accepted TCP connection and registers it for active management.
        /// </summary>
        /// <param name="acceptedConnection">The accepted connection information provided by the TCP listener.</param>
        /// <remarks>Creates a connection worker and context, registers event handlers for message receipt, closure and errors, and starts the worker. The new connection is added to the active connections dictionary. Any exceptions encountered during setup are published to the event bus.</remarks>
        public void ProcessNewConnection(AcceptedConnection acceptedConnection)
        {
            try
            {
                // Create a linked cancellation token source for the connection
                CancellationTokenSource connectionCts =
                    CancellationTokenSource.CreateLinkedTokenSource(_serverCts.Token);

                // Create the connection worker
                TcpConnectionWorker worker = new TcpConnectionWorker(
                    acceptedConnection,
                    connectionCts.Token,
                    _packetFactory,
                    _packetSerializer,
                    _packetLimits
                );

                // Create the connection context
                ConnectionContext context = new ConnectionContext(
                    acceptedConnection,
                    worker,
                    _serverCts
                );

                // Add to active connections
                bool added = _activeConnections.TryAdd(acceptedConnection.connId, context);
                if (!added)
                {
                    throw new Exception(
                        $"Connection ID {acceptedConnection.connId} already exists in active connections."
                    );
                }

                // Register handlers
                worker.MessageReceived += OnWorkerMessageReceived;
                worker.ConnectionClosed += OnWorkerConnectionClosed;
                worker.ErrorOccurred += OnWorkerErrorOccurred;

                // Start processing
                worker.Start();

                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Network,
                    new EventReason($"New connection registered: {acceptedConnection.connId}")
                );

            }
            catch (Exception ex)
            {
                // Log failure
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason($"Exception while processing new connection {acceptedConnection.connId}: {ex.Message}")
                );

                // Rethrow here if higher-level code should react to this event, otherwise swallow to prevent crashing the server
            }
        }

        /// <summary>
        /// Sends a protocol message to the specified client connection.
        /// </summary>
        /// <param name="client">The identifier of the client connection to which the message will be sent.</param>
        /// <param name="msg">The protocol envelope containing the message to send to the client.</param>
        /// <remarks>If the specified client connection is not active, the message is not sent and an error event is published to the event bus. This method logs both successful and failed send attempts using the event bus.</remarks>
        public void SendToClient(ConnectionId client, TransportEnvelope msg)
        {
            try
            {
                // Try to find the connection worker for the specified client connection ID in the active connections dictionary.
                bool result = _activeConnections.ContainsKey(client);

                if (result)
                {
                    // Fire off log message to the eventBus
                    ConnectionContext connection = _activeConnections[client];
                    connection.Worker.SendMessage(msg);

                    // Log the event
                    EventBusHelper.PublishEvent(
                        _eventBus,
                        EventMessageType.Network,
                        new EventReason($"Message sent to client: {client}, MessageID: {msg.MessageId}, Type: {msg.MessageType}")
                    );
                }
            }
            catch
            {
                // Log failure event to eventBus here.
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason($"Failed to send message to client {client}: {msg.MessageType} (ID: {msg.MessageId})")
                );
            }
        }       

        /// <summary>
        /// Starts listening for incoming client connections on the configured network endpoint.
        /// </summary>
        /// <returns>True if the listener is successfully started or is already running; otherwise, false.</returns>
        /// <remarks>If the listener is already running, this method returns true without taking further action. If starting the listener fails, the method returns false and does not throw an exception.</remarks>
        public bool StartListener()
        {
            // Check if the listener is already off, if so return true.
            if (IsListeningForConnections == true) return true;

            try
            {
                // Initiate the TCP listener on the configured endpoint.
                _tcpConnectionListener.Start();

                IsListeningForConnections = true;

                // Log the event
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Network,
                    new ListenerStateChangedEvent(IsListeningForConnections)
                );
                
            }
            catch
            {
                IsListeningForConnections = false;

                // Log failure event to eventBus here.
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason($"Failed to start TCP listener on {_listenerEndPoint}")
                );
                return false;
            }

            return true;
        }
       
        /// <summary>
        /// Stops the server from accepting new client connections.
        /// </summary>
        /// <returns>True if the server successfully stops accepting new clients or was already stopped; otherwise, false.</returns>
        /// <remarks>If the server is already in the stopped state, this method returns true immediately. If the stopping process encounters any errors, they are logged as error events, and the method returns false.</remarks>
        public bool StopListener()
        {
            // Check if the listener is already off, if so return true.
            if (IsListeningForConnections == false) return true;

            try
            {
                // Initiate the TCP listener on the configured endpoint.
                _ = _tcpConnectionListener.StopAsync();

                // Log success event to eventBus here.
                IsListeningForConnections = false;

                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Network,
                    new ListenerStateChangedEvent(IsListeningForConnections)
                );
            }
            catch
            {
                // Log failure event to eventBus here.
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason($"Failed to stop TCP listener on {_listenerEndPoint}")
                );
                return false;
            }
            return true;
        }

        /// <summary>
        /// Initiates a graceful shutdown of the server, terminating all active client connections and stopping the acceptance of new connections.
        /// </summary>
        /// <remarks>This method signals all connection workers to terminate, ensuring that existing client connections are closed cleanly. Any errors encountered during the shutdown process are logged to the event bus. After calling this method, the server will no longer accept new client connections.</remarks>
        public void ShutdownSupervisor()
        {
            try
            {
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.System,
                    new EventReason("Server shutdown initiated, all client connections have been issued terminatation signals")
                );

                // First, stop accepting new client connections to prevent new clients from connecting while the shutdown process is underway.
                StopListener();

                // Issue cancellation request on servers cancellation token source to signal all connection workers to stop processing
                // messages and terminate client connections gracefully.
                _serverCts.Cancel();

                // Go through the active connections and stop each connection worker to terminate all client connections gracefully,
                // while also logging the shutdown event to the eventBus.
                foreach (var context in _activeConnections.Values)
                {
                    context.Worker.Stop();
                }
                
            }
            catch
            {
                // Log erros during shutdown to eventBus here.
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason("Exception occurred during server shutdown")
                );
            }
        }

        /// <summary>
        /// Validates whether the server is currently in a state that can accept and process incoming messages.
        /// </summary>
        /// <returns> 
        /// ValidationResult: An object which contains a boolean indicating pass or fail, along with an optional fail message.
        /// </returns>
        private ValidationResult ValidateSystemCanAcceptMessages()
        {
            // Check if the server is in the shutting down state. If so, provide rejection response.
            if(_lifecycle.IsShuttingDown)
            {
                // This is an error level event. Create appropriate response.
                SystemResponse response = new SystemResponse(SystemResponseType.ServerShuttingDown,
                    SystemResponseSeverity.Error,
                    "The server is shutting down and cannot process messages at this time.",
                    retryable: false);

                ValidationResult result = ValidationResult.Invalid(response);
                return result;
            }

            // Check if the server is in the maintenance state. If so, provide rejection response.
            if (_lifecycle.IsInMaintenance)
            {
                // This is a warning level event. Create appropriate response.
                SystemResponse response = new SystemResponse(SystemResponseType.ServerMaintenance,
                    SystemResponseSeverity.Warning,
                    "The server is currently in maintenance mode and cannot process messages at this time.",
                    retryable: true);

                ValidationResult result = ValidationResult.Invalid(response);
                return result;
            }

            // Validation passed - create a positive validationResult to return to caller.
            ValidationResult validResult = ValidationResult.Valid();
            return validResult;
        }

        /// <summary>
        /// Starts the network supervisor by initiating the TCP listener and enabling acceptance of incoming client connections.
        /// </summary>
        public void Start()
        {
            // Check if the commandPipeline has been set
            if(_commandPipeline == null )
            {
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason("Cannot start network supervisor, command pipeline not set")
                );
                return;
            }

            // Check if the network is already running.
            if (IsListeningForConnections == true)
            {
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.System,
                    new EventReason($"Network supervisor already started on {_listenerEndPoint}")
                );
                return;
            }

            EventBusHelper.PublishEvent
                (
                    _eventBus,
                    EventMessageType.System,
                    new EventReason($"Network supervisor starting on {_listenerEndPoint}")
                );

            // Subscribe to and listen for outbound message events
            _subscriptions.Add(_eventBus.Subscribe<NetworkEvents.OutboundMessageEvent>(
                eventType: EventMessageType.Network,
                handler: OnOutboundMessageRequested
            ));

            // Subscribe to server lifecycle events to react to state changes
            _subscriptions.Add(_eventBus.Subscribe<ServerStateChangedEvent>(
                eventType: EventMessageType.System,
                handler: OnServerStateChanged
            ));

            // Subscribe to start listener requests from other parts of the system
            _subscriptions.Add(_eventBus.Subscribe<StartListnerRequestEvent>(
                eventType: EventMessageType.Network,
                handler: OnStartListenerRequested
            ));

            // Subscribe to stop listener requests from other parts of the system
            _subscriptions.Add(_eventBus.Subscribe<StopListenerRequestEvent>(
                eventType: EventMessageType.Network,
                handler: OnStopListenerRequested
            ));

            return;
        }

        /// <summary>
        /// Stops the network supervisor and performs necessary cleanup operations.
        /// </summary>
        public void Stop()
        {
            EventBusHelper.PublishEvent(
                _eventBus,
                EventMessageType.System,
                new EventReason($"Network supervisor stopping on {_listenerEndPoint}")
            );

            foreach(var subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();

            ShutdownSupervisor();
        }

        /// <summary>
        /// Sets the command pipeline orchestrator that the network supervisor will use to process incoming messages from clients.         /// </summary>
        /// <param name="pipeline">A reference to the commandPipeline to be used.</param>
        /// <returns> bool: True when command pipeline was successfully set.</returns>
        public bool SetCommandPipeline(CommandPipelineOrchestrator pipeline)
        {
            // Validate that the incomming pipeline is not null
            if (pipeline == null) return false;

            // Validate that the pipeline has not yet been set.
            // We do not want to allow changing the pipeline after it has been set.
            if (_commandPipeline != null) return false;

            // Set the pipeline
            _commandPipeline = pipeline;

            return true;
        }

        /// <summary>
        /// Sends a message to multiple client connections specified by their connection IDs.
        /// </summary>
        /// <param name="clients">A list of the clients to send to</param>
        /// <param name="msg">The message to send to each client</param>
        public void SendToMultipleClients(IEnumerable<ConnectionId> clients, TransportEnvelope msg)
        {
            if(clients == null || !clients.Any())
            {
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason($"No recipients specified for outbound message: {msg.MessageType} (ID: {msg.MessageId})")
                );
                return;
            }


            foreach (var client in clients)
            {
                try
                {
                    // Try to find the connection worker for the specified client connection ID in the active connections dictionary.
                    bool result = _activeConnections.ContainsKey(client);

                    if (result)
                    {
                        // Fire off log message to the eventBus
                        ConnectionContext connection = _activeConnections[client];
                        connection.Worker.SendMessage(msg);

                        // Log the event
                        EventBusHelper.PublishEvent(
                            _eventBus,
                            EventMessageType.Network,
                            new EventReason($"Message sent to client {client}: {msg.MessageType} (ID: {msg.MessageId})")
                        );
                    }
                }
                catch
                {
                    // Log failure event to eventBus here.
                    EventBusHelper.PublishEvent(
                        _eventBus,
                        EventMessageType.Error,
                        new EventReason($"Failed to send message to client {client}: {msg.MessageType} (ID: {msg.MessageId})")
                    );
                }
            }

        }

        #region Event Handlers

        /// <summary>
        /// Handles outbound message requests from any part of the system that needs to push
        /// a message to clients without going through the inbound command pipeline.
        /// </summary>
        private void OnOutboundMessageRequested(NetworkEvents.OutboundMessageEvent evnt)
        {
            var envelope = new TransportEnvelope(
                messageId: _messageIdGenerator.New(),
                sessionId: null,
                messageType: evnt.MessageType,
                flags: MessageFlags.None,
                payload: Encoding.UTF8.GetBytes(evnt.Message),
                connectionId: default
            );

            SendToMultipleClients(evnt.Recipients, envelope);
        }

        /// <summary>
        /// Handles errors that occur in a connection worker by terminating the associated connection and publishing an error event.
        /// </summary>
        /// <param name="sender">The source of the error event. Must be an instance of <see cref="IConnectionWorker"/> representing the connection worker where the error occurred.</param>
        /// <param name="e">The exception that was thrown by the connection worker.</param>
        /// <remarks>If the connection worker is found in the active connections, the connection is marked for closure and an error event is published. If the connection is not found, an error event is still published indicating the missing connection. This method does not throw exceptions if the sender is not an IConnectionWorker.</remarks>
        private void OnWorkerErrorOccurred(object? sender, Exception e)
        {
            // Check if the sender of the event is a connection worker, if not return.
            if (sender is not IConnectionWorker worker) return;


            // Try to find the connection worker for the connection that experienced the error in the active connections dictionary
            // using the connection ID as the key, and if found, cancel the connection's cancellation token source and stop the
            // connection worker to terminate the connection.
            if (_activeConnections.TryGetValue(worker.ConnId, out var context))
            {
                context.CancellationSource.Cancel();
                context.Worker.Stop();

                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason($"Error occurred in connection worker {worker.ConnId}, connection marked for closure: {e.Message}")
                );
            }
            else
            {
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason($"Error occurred in connection worker {worker.ConnId}, but connection not found in active connections: {e.Message}")
                );
            }
        }

        /// <summary>
        /// Handles the closure of a worker connection by removing it from the active connections and performing necessary cleanup.
        /// </summary>
        /// <param name="sender">The source of the event. Must be an object implementing the <see cref="IConnectionWorker"/> interface representing the closed connection.</param>
        /// <param name="e">An <see cref="EventArgs"/> instance containing the event data.</param>
        /// <remarks>If the connection is found in the active connections, its associated resources are disposed and a network event is published. If not found, an error event is published.</remarks>
        private void OnWorkerConnectionClosed(object? sender, EventArgs e)
        {
            // Check if the sender of the event is a connection worker, if not return.
            if (sender is not IConnectionWorker worker) return;

            // Try to remove the connection worker for the closed connection from the active connections dictionary using
            // the connection ID as the key, and if successful, dispose of the connection's cancellation token source and
            // log the connection closure event to the eventBus.
            if (_activeConnections.TryRemove(worker.ConnId, out var context))
            {
                context.CancellationSource.Dispose();

                // log connection closed
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Network,
                    new EventReason($"Connection {worker.ConnId} closed and removed from active connections")
                );

                _eventBus.Publish(EventMessageType.Network, new NetworkEvents.ClientDisconnectedEvent(worker.ConnId));
            }
            else
            {
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason($"Connection {worker.ConnId} closed, but connection not found in active connections")
                );
            }
        }

        /// <summary>
        /// Handles messages received from a worker by publishing a corresponding event to the event bus.
        /// </summary>
        /// <param name="sender">The source of the event. This parameter can be null.</param>
        /// <param name="e">The protocol envelope containing the message data received from the client.</param>
        private void OnWorkerMessageReceived(object? sender, TransportEnvelope e)
        {
            // Check if we are in an acceptable state to receive messages.
            ValidationResult result = ValidateSystemCanAcceptMessages();

            if (result.IsValid == false)
            {
                if(result.RejectionResponse == null)
                {
                    EventBusHelper.PublishEvent(
                        _eventBus,
                        EventMessageType.Error,
                        new EventReason($"Message received from worker but validation failed and no rejection response provided: {e.MessageType} (ID: {e.MessageId})")
                    );
                    return;
                }

                // If not, send rejection response back to client and return without processing message.
                if (sender is IConnectionWorker worker)
                {
                    TransportEnvelope responseEnvelope = new TransportEnvelope(
                        messageId: _messageIdGenerator.New(),
                        sessionId: null,
                        messageType: TransportMessageType.Error,
                        flags: MessageFlags.None,
                        payload: Encoding.UTF8.GetBytes(result.RejectionResponse!.Message),
                        connectionId: worker.ConnId
                    );
                    worker.SendMessage(responseEnvelope);
                }
                else
                {
                    EventBusHelper.PublishEvent(
                        _eventBus,
                        EventMessageType.Error,
                        new EventReason($"Received message from worker, but sender is not a connection worker, cannot send rejection response: {e.MessageType} (ID: {e.MessageId})")
                    );
                }
                return;
            }

            // Fire off an event for the logger
            EventBusHelper.PublishEvent(
                _eventBus,
                EventMessageType.Network,
                new EventReason($"Message received from client: {e.MessageType} (ID: {e.MessageId}, {e.Payload.Length} bytes)")
            );

            // Forward into command / message pipeline
            if (_commandPipeline != null)
            {
                _commandPipeline.ProcessMessage(e);
            }
            else
            {
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason($"Cannot process message from worker, command pipeline not set: {e.MessageType} (ID: {e.MessageId})")
                );
            }
        }

        /// <summary>
        /// Callback invoked when the TCP listener encounters an error.
        /// </summary>
        /// <param name="e">The exception that occurred in the listener.</param>
        /// <remarks>Publishes an error event to the event bus containing the exception message. Intended to be used as the implementation of the <see cref="IListenerErrorHandler"/> interface.</remarks>
        public void OnListenerError(Exception e)
        {
            EventBusHelper.PublishEvent(
                _eventBus,
                EventMessageType.Error,
                new EventReason($"Error occurred in TCP listener: {e.Message}")
            );
        }

        /// <summary>
        /// Event handler for changes in the server's lifecycle state. This method responds to state changes by starting or stopping
        /// the acceptance of client connections based on the new state of the server.
        /// </summary>
        /// <param name="sender">The object that emitted the event.</param>
        /// <param name="e">ServerStateChangedEventData: An object containing info about the event.</param>
        private void OnServerStateChanged(ServerStateChangedEvent e)
        {
            // Log the state change
            EventBusHelper.PublishEvent(
                _eventBus,
                EventMessageType.System,
                new EventReason($"Server state changed from {e.PreviousState} to {e.NewState}")
            );

            if (e.NewState == ServerStateEnum.SHUTTING_DOWN || e.NewState == ServerStateEnum.MAINTENANCE)
            {                
                StopListener(); // Proactively stop accepting connections
            }
            else if(e.NewState == ServerStateEnum.ACTIVE)
            {
                StartListener(); // Start accepting clients if the server becomes active
            }
        }

        /// <summary>
        /// Event handler for when a new client connection is accepted by the TCP listener.
        /// </summary>
        /// <param name="connection">
        /// The new connection that has been accepted.
        /// </param>
        public void OnConnectionAccepted(AcceptedConnection connection)
        {
            // Log the acceptance
            EventBusHelper.PublishEvent(
                _eventBus,
                EventMessageType.Network,
                new EventReason(
                    "Connection accepted",
                    new { connectionId = connection.connId }
                )
            );

            ProcessNewConnection(connection);
        }

        public void OnStartListenerRequested(StartListnerRequestEvent e)
        {
            StartListener();
        }

        public void OnStopListenerRequested(StopListenerRequestEvent e)
        {
            StopListener();
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();

            // Dispose other resources if needed
            _serverCts?.Dispose();
        }

        #endregion
    }
}
