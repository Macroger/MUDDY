using Server.Infrastructure.Identity.ConnectionId;
using Server.Network.Listener;
using Server.Network.Model;
using Server.Network.Worker;
using Shared.Identity;
using Shared.Protocol.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Server.Network.Supervisor
{
    internal class StandardNetworkSupervisor : INetworkSupervisor
    {
        private bool _isRunning;
        private bool _listenerIsRunning;

        private ConcurrentDictionary<ConnectionId, IConnectionWorker> _activeConnections = new ConcurrentDictionary<ConnectionId, IConnectionWorker>();

        private TcpConnectionListener _tcpConnectionListener;

        private IConnectionIdGenerator _connectionIdGenerator;

        private const int serverPort = 30333;

        private IPEndPoint _listenerEndPoint; 

        public StandardNetworkSupervisor(TcpConnectionListener tcpListener)
        {
            _connectionIdGenerator = new ConnectionIdGenerator();
            _listenerEndPoint = new IPEndPoint(IPAddress.Any, serverPort);
            _tcpConnectionListener = new TcpConnectionListener(_listenerEndPoint, this, _connectionIdGenerator);
        }

        public bool BroadcastMessage(MessageEnvelope msg)
        {

            // Loop through the active connections and send the message to each client using the connection worker.
            foreach (var worker in _activeConnections.Values)
            {
                try
                {
                    worker.SendMessage(msg);
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

        public bool CheckNetworkIsRunning()
        {
            return _isRunning;
        }

        public void CloseConnection(ConnectionId connectionId, ConnectionCloseReason reason)
        {
            // Check if the connection exists in the active connections dictionary.
            IConnectionWorker? connectionWorker;
            try
            {
                _activeConnections.TryGetValue(connectionId, out connectionWorker);

                if (connectionWorker != null)
                {
                    // Fire off log message to the eventBus
                    connectionWorker.Close(reason);
                    _activeConnections.TryRemove(connectionId, out _);
                }
                else
                {
                    // Log failure event to eventBus here.
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

        public void ProcessNewConnection(AcceptedConnection acceptedConnection)
        {
            // Create a new connection worker for the accepted connection and add it to the active connections dictionary.
            IConnectionWorker connectionWorker = new ConnectionWorker(acceptedConnection);

            try
            {
                // Add the connection worker to the active connections dictionary using the connection ID as the key.
                bool result = _activeConnections.TryAdd(acceptedConnection.Id, connectionWorker);

                connectionWorker.Start();
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

        public void SendToClient(ConnectionId client, MessageEnvelope msg)
        {
            try
            {
                // Try to find the connection worker for the specified client connection ID in the active connections dictionary.
                bool result = _activeConnections.ContainsKey(client);

                if (result)
                {
                    // Fire off log message to the eventBus
                    IConnectionWorker connectionWorker = _activeConnections[client];
                    connectionWorker.SendMessage(msg);
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
            if (_listenerIsRunning == true) return true;
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
                _listenerIsRunning = true;                
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
            if (_listenerIsRunning == false) return true;
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
                _listenerIsRunning = false;
            }

            return true;
        }
    }
}
