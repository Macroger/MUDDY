using Server.Core.Infrastructure.Identity.ConnectionId;
using Server.Core.Network.Packet;
using Server.Core.Network.Listener;
using Server.Core.Network.Model;
using Shared.Protocol.Transport;
using Server.Core.Network.Worker;
using Shared.Identity;
using Shared.Protocol.Types;
using System.Collections.Concurrent;
using System.Net;
using Shared.EventBus;
using Shared.Types;

namespace Server.Core.Network.Supervisor
{
    public class StandardNetworkSupervisor : INetworkSupervisor, IListenerErrorHandler
    {
        #region Dependency Injections
        private TcpConnectionListener _tcpConnectionListener;
        private IConnectionIdGenerator _connectionIdGenerator;
        private MuddyPacketFactory _packetFactory;
        private MuddyProtocolLimits _packetLimits;
        private MuddyPacketSerializer _packetSerializer;
        private IEventBus _eventBus;
        private CancellationTokenSource _serverCts;
        private IPEndPoint _listenerEndPoint;
        #endregion

        private ConcurrentDictionary<ConnectionId, ConnectionContext> _activeConnections = new ConcurrentDictionary<ConnectionId, ConnectionContext>();

        private const int serverPort = 33300;

        public bool IsListeningForConnections { get; private set; } = false;

        public StandardNetworkSupervisor(
            TcpConnectionListener tcpListener, 
            IEventBus bus, 
            int port)
        {
            if(port <= 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port), "Port number must be between 1 and 65535.");

            _connectionIdGenerator = new ConnectionIdGenerator();
            _listenerEndPoint = new IPEndPoint(IPAddress.Any, port);
            _tcpConnectionListener = new TcpConnectionListener(_listenerEndPoint, this, _connectionIdGenerator, this);
            _serverCts = new CancellationTokenSource();
            _packetFactory = new MuddyPacketFactory();
            _packetLimits = new MuddyProtocolLimits();
            _packetSerializer = new MuddyPacketSerializer(_packetLimits);
            _eventBus = bus;
        }

        public void BroadcastMessage(Shared.Protocol.Types.ProtocolEnvelope msg)
        {
            EventBusHelper.PublishEvent(
                _eventBus,
                EventMessageType.Network,
                new EventReason(
                    "Broadcasting message to all active connections",
                    new { messageId = msg.MessageId, messageType = msg.MessageType, activeConnectionCount = _activeConnections.Count }
                )
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
                        new EventReason(
                            "Failed to send broadcast message to client",
                            new { connectionId = connection.ClientConnection.connId, messageId = msg.MessageId, messageType = msg.MessageType }
                        )
                    );
                    continue;
                }
            }
        }


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
                        new EventReason(
                        "Connection marked for closure",
                        new { connectionId, reason }
                    ));
                }
                else
                {
                    EventBusHelper.PublishEvent(
                        _eventBus,
                        EventMessageType.Network,
                        new EventReason(
                        "Connection not found, cannot close connection",
                        new { connectionId, reason }
                    ));
                }
            }
            catch (Exception ex)
            {
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason(
                    "Exception while closing connection",
                    new { connectionId, reason, ex.Message }
                ));
            }
        }

        
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
                    new EventReason(
                        "New connection registered",
                        new { acceptedConnection.connId }
                    )
                );

            }
            catch (Exception ex)
            {
                // Log failure
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason(
                        "Exception while processing new connection",
                        new { acceptedConnection.connId, ex.Message }
                    )
                );

                // Rethrow here if higher-level code should react to this event, otherwise swallow to prevent crashing the server
            }
        }

        /// <summary>
        /// Handles errors that occur in a connection worker by terminating the associated connection and publishing an
        /// error event.
        /// </summary>
        /// <remarks>If the connection worker is found in the active connections, the connection is marked
        /// for closure and an error event is published. If the connection is not found, an error event is still
        /// published indicating the missing connection. This method does not throw exceptions if the sender is not an
        /// IConnectionWorker.</remarks>
        /// <param name="sender">The source of the error event. Must be an instance of IConnectionWorker representing the connection worker
        /// where the error occurred.</param>
        /// <param name="e">The exception that was thrown by the connection worker.</param>
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
                    new EventReason(
                        "Error occurred in connection worker, connection marked for closure",
                        new { connectionId = worker.ConnId, errorMessage = e.Message }
                    )
                );
            }
            else
            {
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason(
                        "Error occurred in connection worker, but connection not found in active connections",
                        new { connectionId = worker.ConnId, errorMessage = e.Message }
                    )
                );
            }
        }

        /// <summary>
        /// Handles the closure of a worker connection by removing it from the active connections and performing
        /// necessary cleanup.
        /// </summary>
        /// <remarks>If the connection is found in the active connections, its associated resources are
        /// disposed and a network event is published. If not found, an error event is published. This method is
        /// intended to be used as an event handler for connection closure events.</remarks>
        /// <param name="sender">The source of the event. Must be an object implementing the IConnectionWorker interface representing the
        /// closed connection.</param>
        /// <param name="e">An EventArgs instance containing the event data.</param>
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
                    new EventReason(
                        "Connection closed and removed from active connections",
                        new { connectionId = worker.ConnId }
                    )
                );
            }
            else
            {
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason(
                        "Connection closed, but connection not found in active connections",
                        new { connectionId = worker.ConnId }
                    )
                );
            }
        }

        /// <summary>
        /// Handles messages received from a worker by publishing a corresponding event to the event bus.
        /// </summary>
        /// <param name="sender">The source of the event. This parameter can be null.</param>
        /// <param name="e">The protocol envelope containing the message data received from the client.</param>
        private void OnWorkerMessageReceived(object? sender, Shared.Protocol.Types.ProtocolEnvelope e)
        {            
            // Forward into command / message pipeline
            // or publish onto EventBus
            // For now just log it and move on
            EventBusHelper.PublishEvent(
                _eventBus,
                EventMessageType.Network,
                new EventReason(
                    "Message received from client",
                    new { messageId = e.MessageId, messageType = e.MessageType }
                )
            );
        }

        public void OnListenerError(Exception e)
        {
            EventBusHelper.PublishEvent(
                _eventBus,
                EventMessageType.Error,
                new EventReason(
                    "Error occurred in TCP listener",
                    new { errorMessage = e.Message }
                )
            );
        }

        /// <summary>
        /// Sends a protocol message to the specified client connection.
        /// </summary>
        /// <remarks>If the specified client connection is not active, the message is not sent and an
        /// error event is published to the event bus. This method logs both successful and failed send attempts using
        /// the event bus.</remarks>
        /// <param name="client">The identifier of the client connection to which the message will be sent.</param>
        /// <param name="msg">The protocol envelope containing the message to send to the client.</param>
        public void SendToClient(ConnectionId client, Shared.Protocol.Types.ProtocolEnvelope msg)
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
                        new EventReason(
                            "Message sent to client",
                            new { connectionId = client, messageId = msg.MessageId, messageType = msg.MessageType }
                        )
                    );
                }
            }
            catch
            {
                // Log failure event to eventBus here.
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason(
                        "Failed to send message to client",
                        new { connectionId = client, messageId = msg.MessageId, messageType = msg.MessageType }
                    )
                );
            }
        }       

        /// <summary>
        /// Starts listening for incoming client connections on the configured network endpoint.
        /// </summary>
        /// <remarks>If the listener is already running, this method returns <see langword="true"/>
        /// without taking further action. If starting the listener fails, the method returns <see langword="false"/>
        /// and does not throw an exception.</remarks>
        /// <returns>true if the listener is successfully started or is already running; otherwise, false.</returns>
        public bool StartAcceptingClients()
        {
            // Check if the network is already running, if so return true.
            if (IsListeningForConnections == true) return true;
            try
            {
                // Initiate the TCP listener on the configured endpoint.
                _tcpConnectionListener.Start();

                // Log success event to eventBus here.
                IsListeningForConnections = true;

                // Log the event
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Network,
                    new EventReason(
                        "TCP listener started successfully",
                        new { endpoint = _listenerEndPoint }
                    )
                );
            }
            catch
            {
                // Log failure event to eventBus here.
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason(
                        "Failed to start TCP listener",
                        new { endpoint = _listenerEndPoint }
                    )
                );
                return false;
            }

            return true;
        }

       
        /// <summary>
        /// Stops the server from accepting new client connections.
        /// </summary>
        /// <remarks>This method does not disconnect existing clients. It only prevents new connections
        /// from being accepted. If the server is already not accepting clients, the method returns true
        /// immediately.</remarks>
        /// <returns>true if the server successfully stops accepting new clients or was already stopped; otherwise, false.</returns>
        public bool StopAcceptingClients()
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
                    new EventReason(
                        "TCP listener stopped successfully",
                        new { endpoint = _listenerEndPoint }
                    )
                );
            }
            catch
            {
                // Log failure event to eventBus here.
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason(
                        "Failed to stop TCP listener",
                        new { endpoint = _listenerEndPoint }
                    )
                );
                return false;
            }
            return true;
        }


        /// <summary>
        /// Initiates a graceful shutdown of the server, terminating all active client connections and stopping the
        /// acceptance of new connections.
        /// </summary>
        /// <remarks>This method signals all connection workers to terminate, ensuring that existing
        /// client connections are closed cleanly. Any errors encountered during the shutdown process are logged to the
        /// event bus. After calling this method, the server will no longer accept new client connections.</remarks>
        public void ShutdownServer()
        {
            try
            {
                // First, stop accepting new client connections to prevent new clients from connecting while the shutdown process is underway.
                StopAcceptingClients();

                // Issue cancellation request on servers cancellation token source to signal all connection workers to stop processing
                // messages and terminate client connections gracefully.
                _serverCts.Cancel();

                // Go through the active connections and stop each connection worker to terminate all client connections gracefully,
                // while also logging the shutdown event to the eventBus.
                foreach (var context in _activeConnections.Values)
                {
                    context.Worker.Stop();
                }
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.System,
                    new EventReason(
                        "Server shutdown initiated, all client connections have been issued terminatation signals",
                        new { }
                    )
                );
            }
            catch
            {
                // Log erros during shutdown to eventBus here.
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason(
                        "Exception occurred during server shutdown",
                        new { }
                    )
                );
            }
        }
    }
}
