using Server.Infrastructure.Identity.ConnectionId;
using Server.Network.Packet;
using Server.Network.Listener;
using Server.Network.Model;
using Shared.Protocol.Transport;
using Server.Network.Worker;
using Shared.Identity;
using Shared.Protocol.Types;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;

namespace Server.Network.Supervisor
{
    public class StandardNetworkSupervisor : INetworkSupervisor
    {
        private ConcurrentDictionary<ConnectionId, ConnectionContext> _activeConnections = new ConcurrentDictionary<ConnectionId, ConnectionContext>();

        private TcpConnectionListener _tcpConnectionListener;

        private IConnectionIdGenerator _connectionIdGenerator;

        private const int serverPort = 30333;

        private IPEndPoint _listenerEndPoint;
        private CancellationTokenSource _serverCts;
        private MuddyPacketFactory _packetFactory;
        private MuddyProtocolLimits _packetLimits;
        private MuddyPacketSerializer _packetSerializer;

        public bool IsListeningForConnections { get; private set; } = false;

        public StandardNetworkSupervisor(TcpConnectionListener tcpListener)
        {
            _connectionIdGenerator = new ConnectionIdGenerator();
            _listenerEndPoint = new IPEndPoint(IPAddress.Any, serverPort);
            _tcpConnectionListener = new TcpConnectionListener(_listenerEndPoint, this, _connectionIdGenerator);
            _serverCts = new CancellationTokenSource();
            _packetFactory = new MuddyPacketFactory();
            _packetLimits = new MuddyProtocolLimits();
            _packetSerializer = new MuddyPacketSerializer(_packetLimits);
        }

        public void BroadcastMessage(MessageEnvelope msg)
        {
            // Loop through the active connections and send the message to each client using the connection worker.
            foreach (var connection in _activeConnections.Values)
            {
                try
                {
                    connection.Worker.SendMessage(msg);
                }
                catch
                {
                    // Log failure event to eventBus here.
                }
                finally
                {
                    // Log success event to eventBus here.
                }
            }
        }


        public void CloseConnection(ConnectionId connectionId, ConnectionCloseReason reason)
        {
            // Check if the connection worker for the specified connection ID exists in the active connections dictionary,
            // and if found, cancel the connection's cancellation token source and stop the connection worker to terminate
            // the connection, while also logging the close reason to the eventBus.
            if (_activeConnections.TryGetValue(connectionId, out var context))
            {
                // log close reason
                context.CancellationSource.Cancel();
                context.Worker.Stop();
            }
        }

        public void ProcessNewConnection(AcceptedConnection acceptedConnection)
        {          
            try
            {
                // Create a linked cancellation token source for the connection, which will be cancelled when either
                // the server is shutting down or the connection is closed.
                CancellationTokenSource connectionCts = CancellationTokenSource.CreateLinkedTokenSource(_serverCts.Token);

                // Create a new connection worker for the accepted connection.
                TcpConnectionWorker worker = new TcpConnectionWorker(
                    acceptedConnection,
                    connectionCts.Token,
                    _packetFactory,
                    _packetSerializer,
                    _packetLimits);

                // Create a new connection context, containing relevant information.
                ConnectionContext context = new ConnectionContext(
                    acceptedConnection,
                    worker,
                    _serverCts);

                // Attempt to add the connection worker for the accepted connection to the active connections dictionary using the connection ID as the key.
                bool result = _activeConnections.TryAdd(acceptedConnection.connId, context);

                // Chck if the connection worker was successfully added to the active connections dictionary, if not throw an exception.
                if (!result) throw new Exception($"Failed to add connection worker for connection ID {acceptedConnection.connId} to active connections dictionary.");

                // Register event handlers for the connection worker's MessageReceived, ConnectionClosed, and ErrorOccurred events to handle incoming messages, connection closures, and errors.
                worker.MessageReceived += OnWorkerMessageReceived;
                worker.ConnectionClosed += OnWorkerConnectionClosed;
                worker.ErrorOccurred += OnWorkerErrorOccurred;

                // Start the connection worker to begin processing messages from the client.
                worker.Start();
            }
            catch
            {
                // Log the failure event to the eventBus here.
            }
            finally
            {
                // Log the success event to the eventBus here.
            }
        }

        private void OnWorkerErrorOccurred(object? sender, Exception e)
        {
            // Check if the sender of the event is a connection worker, if not return.
            if (sender is not IConnectionWorker worker) return;

            // log error

            // Try to find the connection worker for the connection that experienced the error in the active connections dictionary
            // using the connection ID as the key, and if found, cancel the connection's cancellation token source and stop the
            // connection worker to terminate the connection.
            if (_activeConnections.TryGetValue(worker.ConnId, out var context))
            {
                context.CancellationSource.Cancel();
                context.Worker.Stop();
            }
        }

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
            }
        }

        private void OnWorkerMessageReceived(object? sender, MessageEnvelope e)
        {
            throw new NotImplementedException();

            // Forward into command / message pipeline
            // or publish onto EventBus

        }

        public void SendToClient(ConnectionId client, MessageEnvelope msg)
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
                }
            }
            catch
            {
                // Log failure event to eventBus here.
            }
            finally
            {
                // Log success event to eventBus here.
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
            }
            catch
            {
                // Log failure event to eventBus here.
                return false;
            }
            finally
            {
                // Log success event to eventBus here.
                IsListeningForConnections = true;                
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
            }
            catch
            {
                // Log failure event to eventBus here.
                return false;
            }
            finally
            {
                // Log success event to eventBus here.
                IsListeningForConnections = false;
            }

            return true;
        }


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
            }
            catch
            {
                // Log erros during shutdown to eventBus here.
            }
            finally
            {
                // Log successful shutdown event to eventBus here.
            }

        }

    }
}
