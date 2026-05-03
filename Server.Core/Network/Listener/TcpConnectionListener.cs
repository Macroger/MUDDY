// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.Infrastructure.Identity.ConnectionId;
using Server.Core.Network.Model;
using Server.Core.Network.Supervisor;
using Shared.Identity;
using System.Net;
using System.Net.Sockets;

namespace Server.Core.Network.Listener
{
    public class TcpConnectionListener
    {
        private readonly INetworkSupervisor _supervisor;
        private readonly IConnectionIdGenerator _connectionIdGenerator;
        private IListenerErrorHandler _listenerErrorHandler;
        private IConnectionAcceptedHandler _connectionAcceptedHandler;

        private IPEndPoint _endPoint;
        private CancellationTokenSource? _cts;
        private readonly TcpListener _listener;
        private Task? _acceptClientsTask;

        public bool ListenerIsRunning { get; private set; } = false;
        public IPEndPoint ConnectionEndPoint { get; init; }

        public TcpConnectionListener(
            IPEndPoint localEndPoint,
            INetworkSupervisor supervisor,
            IConnectionIdGenerator connIdGenerator,
            IListenerErrorHandler listenerErrorHandler,
            IConnectionAcceptedHandler connectionAcceptedHandler
            )
        {
            _supervisor = supervisor;
            _connectionIdGenerator = connIdGenerator;
            _listenerErrorHandler = listenerErrorHandler;
            _connectionAcceptedHandler = connectionAcceptedHandler;
            _endPoint = localEndPoint;
            _listener = new TcpListener(_endPoint);
            ConnectionEndPoint = _endPoint;
        }

        public void Start()
        {
            String self = "TcpConnectionListener.Start";

            try
            {
                // Check if already listening.
                if (ListenerIsRunning) return;

                _cts = new CancellationTokenSource();
                _listener.Start();
                _acceptClientsTask = Task.Run(() => AcceptNewClients(_cts.Token));
                ListenerIsRunning = true;
            }
            catch (SocketException ex)
            {
                // An unexpected socket error has occurred.
                // Let the supervisor decide how to handle this - whether to attempt a restart, or to shut down the listener permanently.
                _listenerErrorHandler.OnListenerError(ex);
            }
            catch (Exception ex)
            {
                // General exception has occured, could be anything.
                ListenerIsRunning = false;
                _listener.Stop();
                throw new Exception($"[{self}] Error. Encountered exception while accepting new clients: {ex.Message}");
            }
        }

        /// <summary>
        /// Asynchronously stops the listener and releases associated resources.
        /// </summary>
        public async Task StopAsync()
        {
            if (ListenerIsRunning == false) return;

            ListenerIsRunning = false;

            _cts?.Cancel();
            _listener.Stop();

            if (_acceptClientsTask != null && _cts != null) await _acceptClientsTask.ConfigureAwait(false);

            _cts?.Dispose();
            _acceptClientsTask = null;
            _cts = null;

        }

        /// <summary>
        /// Asynchronously accepts incoming TCP client connections until cancellation is requested.
        /// </summary>
        private async Task AcceptNewClients(CancellationToken cancellationToken)
        {
            String self = "TcpConnectionListener.AcceptNewClients";

            // Loop while the cancellation token is not active.
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check is the listener is currently running.
                    if (!ListenerIsRunning) throw new InvalidOperationException($"[{self}] Error. Unable to accept new clients; Listener is not started.");

                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);

                    ConnectionId newConnId = _connectionIdGenerator.New();
                    AcceptedConnection acceptedConnection = new AcceptedConnection(newConnId, client.Client);

                    try
                    {
                        _connectionAcceptedHandler.OnConnectionAccepted(acceptedConnection);
                    }
                    catch (Exception ex)
                    {
                        // If any exceptions happen here we must dispose of the left over client socket.
                        _listenerErrorHandler.OnListenerError(ex);
                        client.Dispose();
                        continue;
                    }

                }
                catch (OperationCanceledException)
                {
                    // Normal stop path: Token has requested cancellation.
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // This can happen when the listener is running and Dispose() is activated.
                    // Break out of the loop to exit.
                    break;
                }
                catch (SocketException ex)
                {
                    // An unexpected socket error has occurred. 
                    // Perform logging, and continue.
                    _listenerErrorHandler.OnListenerError(ex);
                    continue;
                }
                catch (Exception ex)
                {
                    // General exception has occured, could be anything.
                    // Log and continue, to try to allow listener to stay online.
                    _listenerErrorHandler.OnListenerError(ex);
                    continue;
                }
            }
        }
    }
}
