using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Shared.Networking
{
    public class TcpConnectionListener(IPEndPoint connectionEndPoint)
    {
        private readonly INetworkSupervisor _supervisor;        

        private CancellationTokenSource? _cts;
        private readonly TcpListener _listener = new TcpListener(connectionEndPoint);
        private Task? _acceptClientsTask;

        public bool ListenerIsRunning {  get;  private set; }
        public required IPEndPoint ConnectionEndPoint { get; init; } = connectionEndPoint;

        public void Start()
        {
            String self = "TcpConnectionListener.Start";           

            try
            {           
                // Check if already listening.
                if (ListenerIsRunning)
                    return;

                _cts = new CancellationTokenSource();
                _listener.Start();
                ListenerIsRunning = true;
                _acceptClientsTask = Task.Run(() => AcceptNewClients(_cts.Token));
            }
            catch (SocketException ex)
            {
                // An unexpected socket error has occurred.
                // Log message goes here.
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

                    try
                    {
                        _supervisor.AddClientToQueue(client);
                    }
                    catch (Exception)
                    {
                        // If any exceptions happen here we must dispose of the left of client socket.
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
