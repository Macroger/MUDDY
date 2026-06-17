// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline;
using Server.Core.Infrastructure.Identity.ConnectionId;
using Server.Core.Network.Model;
using Server.Core.Network.Supervisor;
using Shared.Identity;
using Shared.Network.Transport;
using Shared.Network.Types;
using System.Net;
using System.Net.Sockets;

namespace Server.Core.Network.Listener.Tests
{
    [TestClass]
    public class TcpConnectionListenerTests
    {
        private class DummySupervisor : INetworkSupervisor
        {
            public AcceptedConnection? LastConnection { get; private set; }
            public bool IsListeningForConnections => true;
            public bool StartListener() => true;
            public bool StopListener() => true;
            public void CloseConnection(ConnectionId connectionId, ConnectionCloseReason reason) { }
            public void ProcessNewConnection(AcceptedConnection connection)
            {
                LastConnection = connection;
            }
            public void BroadcastMessage(PacketEnvelope msg) { }
            public void SendToClient(ConnectionId client, PacketEnvelope msg) { }

            public void SendToMultipleClients(IEnumerable<ConnectionId> clients, PacketEnvelope msg)
            {
                throw new NotImplementedException();
            }

            public bool SetCommandPipeline(CommandPipelineOrchestrator pipeline)
            {
                throw new NotImplementedException();
            }
        }

        private class DummyConnectionIdGenerator : IConnectionIdGenerator
        {
            public ConnectionId New() => new ConnectionId(Guid.NewGuid().ToString());
        }


        private sealed class DummyListenerErrorHandler : IListenerErrorHandler
        {
            public Exception? LastException { get; private set; }

            public void OnListenerError(Exception exception)
            {
                LastException = exception;
            }
        }

        private sealed class DummyListenerNewConnectionHandler : IConnectionAcceptedHandler
        {
            public AcceptedConnection? LastAcceptedConnection { get; private set; }

            public void OnConnectionAccepted(AcceptedConnection connection)
            {
                LastAcceptedConnection = connection;
            }

            public void OnNewConnection(AcceptedConnection connection)
            {
                LastAcceptedConnection = connection;
            }
        }


        private IPEndPoint _localEndPoint = null!;
        private DummySupervisor _supervisor = null!;
        private DummyConnectionIdGenerator _connIdGen = null!;
        private DummyListenerErrorHandler _listenerErrorHandler = null!;
        private DummyListenerNewConnectionHandler _listenerNewConnectionHandler = null!;

        [TestInitialize]
        public void Setup()
        {
            _localEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            _supervisor = new DummySupervisor();
            _connIdGen = new DummyConnectionIdGenerator();
            _listenerErrorHandler = new DummyListenerErrorHandler();
            _listenerNewConnectionHandler = new DummyListenerNewConnectionHandler();
        }

        [TestMethod]
        public void Start_SetsListenerIsRunning_AndStartsListener()
        {
            // Arrange
            var listener = new TcpConnectionListener(_localEndPoint, _supervisor, _connIdGen, _listenerErrorHandler, _listenerNewConnectionHandler);

            // Act
            listener.Start();

            // Assert
            Assert.IsTrue(listener.ListenerIsRunning);

            // Cleanup
            listener.StopAsync().Wait();
        }

        [TestMethod]
        public void Start_WhenAlreadyRunning_DoesNotThrow()
        {
            // Arrange
            var listener = new TcpConnectionListener(_localEndPoint, _supervisor, _connIdGen, _listenerErrorHandler, _listenerNewConnectionHandler);
            listener.Start();

            // Act & Assert
            listener.Start(); // Should not throw

            // Cleanup
            listener.StopAsync().Wait();
        }

        [TestMethod]
        public async Task StopAsync_StopsListenerAndIsIdempotent()
        {
            // Arrange
            var listener = new TcpConnectionListener(_localEndPoint, _supervisor, _connIdGen, _listenerErrorHandler, _listenerNewConnectionHandler);
            listener.Start();

            // Act
            await listener.StopAsync();

            // Assert
            Assert.IsFalse(listener.ListenerIsRunning);

            // Act again (should be idempotent)
            await listener.StopAsync();
        }

        [TestMethod]
        public void Start_WhenSocketException_ReportsErrorToSupervisor()
        {
            // Arrange
            var usedPort = new TcpListener(IPAddress.Loopback, 0);
            usedPort.Start();
            var port = ((IPEndPoint)usedPort.LocalEndpoint).Port;
            var ep = new IPEndPoint(IPAddress.Loopback, port);
            var listener = new TcpConnectionListener(ep, _supervisor, _connIdGen, _listenerErrorHandler, _listenerNewConnectionHandler);

            // Act
            try
            {
                listener.Start();
            }
            catch (SocketException)
            {
                /* consumed exception */
            }

            // Assert
            Assert.IsNotNull(_listenerErrorHandler.LastException);
            Assert.IsInstanceOfType(_listenerErrorHandler.LastException, typeof(SocketException));

            // Cleanup
            usedPort.Stop();
        }


        [TestMethod]
        public async Task StopAsync_WhenNotStarted_DoesNotThrow()
        {
            // Arrange
            var listener = new TcpConnectionListener(
                _localEndPoint,
                _supervisor,
                _connIdGen,
                _listenerErrorHandler,
                _listenerNewConnectionHandler
            );

            // Act & Assert
            await listener.StopAsync();

            // If no exception is thrown, the test passes
        }
    }
}