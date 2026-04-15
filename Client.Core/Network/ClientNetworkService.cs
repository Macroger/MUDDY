using Shared.EventBus;
using Shared.Protocol.Transport;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Client.Core.Network
{
    public class ClientNetworkService
    {
        // Dependencies
        private IEventBus _eventBus;
        private CommandPipelineOrchestrator _commandPipeline;
        private IPacketSerializer _packetSerializer;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private CancellationTokenSource _cts;

        public Task ConnectAsync(string host, int port);
        public Task DisconnectAsync();
        public Task SendMessageAsync(TransportEnvelope envelope);
        private Task ReceiveLoopAsync();
        // Event handlers for connection, error, message received, etc.
    }
}
