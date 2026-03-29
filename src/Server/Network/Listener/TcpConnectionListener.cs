using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Server.Infrastructure.Identity.ConnectionId;
using Server.Network.Model;
using Server.Network.Supervisor;
using Shared.Identity;

namespace Server.Network.Listener
{
    public class TcpConnectionListener(IPEndPoint localEndPoint, INetworkSupervisor supervisor, IConnectionIdGenerator connIdGenerator)
    {
        private readonly INetworkSupervisor _supervisor = supervisor;
        private readonly IConnectionIdGenerator _connectionIdGenerator = connIdGenerator;

        private CancellationTokenSource? _cts;
        private readonly TcpListener _listener = new TcpListener(localEndPoint);
        private Task? _acceptClientsTask;

        public bool ListenerIsRunning {  get;  private set; }
        //public required IPEndPoint ConnectionEndPoint { get; init; } = localEndPoint;

        public void Start()
        {
            String self = "TcpConnectionListener.Start";           

            try
            {           
                // Check if already listening.
                if (ListenerIsRunning) return;

                _cts = new CancellationTokenSource();
                _listener.Start();
                ListenerIsRunning = true;
                _acceptClientsTask = Task.Run(() => AcceptNewClients(_cts.Token));
            }
            catch (SocketException ex)
            {
                // An unexpected socket error has occurred.
                // Log message here.
                ListenerIsRunning = false;
                _listener.Stop();
                throw new SocketException(ex.ErrorCode, $"[{self}] Error. SocketException encountered while opening listener.");
            }
            catch (Exception ex)
            {
                // General exception has occured, could be anything.
                // Log and rethrow.
                ListenerIsRunning = false;
                _listener.Stop();
                throw new Exception($"[{self}] Error. Encountered exception while accepting new clients: {ex.Message}");
            }
        }

        /// <summary>
        /// Asynchronously stops the listener and releases associated resources.
        /// </summary>
        /// <remarks>If the listener is not running, this method returns immediately. Awaiting the
        /// returned task ensures that all pending client connections are properly handled before shutdown. This method
        /// is thread-safe and can be called multiple times safely.</remarks>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public async Task StopAsync()
        {
            if (ListenerIsRunning == false) return;

            ListenerIsRunning = false;

            _cts?.Cancel();
            _listener.Stop();

            if ( _acceptClientsTask != null && _cts != null) await _acceptClientsTask.ConfigureAwait(false);

            _cts?.Dispose();
            _acceptClientsTask = null;
            _cts = null;           

        }

        /// <summary>
        /// Asynchronously accepts incoming TCP client connections until cancellation is requested.
        /// </summary>
        /// <remarks>This method continues to accept clients in a loop until the provided cancellation
        /// token is signaled. Socket and general exceptions are handled internally to allow the listener to remain
        /// operational. The method is intended to be used as part of a long-running listener service.</remarks>
        /// <param name="cancellationToken">A cancellation token that can be used to request the operation to stop accepting new clients.</param>
        /// <returns>A task that represents the asynchronous accept operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the listener is not running when attempting to accept a new client.</exception>
        private async Task AcceptNewClients(CancellationToken cancellationToken)
        {
            String self = "TcpConnectionListener.AcceptNewClients";

            // Loop while the cancellation token is not active.
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check is the listener is currently running.
                    if (!ListenerIsRunning)
                        throw new InvalidOperationException($"[{self}] Error. Unable to accept new clients; Listener is not started.");

                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);

                    ConnectionId newConnId = _connectionIdGenerator.New();
                    AcceptedConnection acceptedConnection = new AcceptedConnection(newConnId, client.Client);

                    try
                    {
                        _supervisor.ProcessNewConnection(acceptedConnection);
                    }
                    catch (Exception)
                    {
                        // If any exceptions happen here we must dispose of the left over client socket.
                        // Log the exception.
                        Console.WriteLine($"[{self}] WARN - Exception occurred while adding client to supervisor collection.");
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
                catch (SocketException)
                {
                    // An unexpected socket error has occurred. 
                    // Perform logging, and continue.
                    // Log message goes here.
                    continue;
                }
                catch (Exception)
                {
                    // General exception has occured, could be anything.
                    // Log and continue, to try to allow listener to stay online.
                    continue;
                }                    
            }
        }
    }
}
