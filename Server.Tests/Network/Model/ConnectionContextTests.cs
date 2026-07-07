// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.Network.Worker;
using Shared.Identity;
using Shared.Network.Transport;
using System.Net;
using System.Net.Sockets;

namespace Server.Core.Network.Model.Tests
{
    [TestClass]
    public class ConnectionContextTests
    {
        private Socket _socket = null!;

        [TestInitialize]
        public void Setup()
        {
            // Use loopback and a random port for a dummy socket
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        }

        [TestCleanup]
        public void Cleanup()
        {
            _socket.Dispose();
        }

        private class DummyWorker : IConnectionWorker
        {
            public bool IsRunning => false;
            public ConnectionId ConnId => new();
            public event EventHandler<MessageEnvelope>? MessageReceived
            {
                add { }
                remove { }
            }
            public event EventHandler? ConnectionClosed
            {
                add { }
                remove { }
            }
            public event EventHandler<Exception>? ErrorOccurred
            {
                add { }
                remove { }
            }
            public bool SendMessage(MessageEnvelope msg) => true;
            public void Start() { }
            public void Stop() { }
        }

        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var accepted = new AcceptedConnection(new Shared.Identity.ConnectionId("abc"), _socket);
            var worker = new DummyWorker();
            var cts = new CancellationTokenSource();

            // Act
            var ctx = new ConnectionContext(accepted, worker, cts);

            // Assert
            Assert.AreEqual(accepted, ctx.ClientConnection);
            Assert.AreEqual(worker, ctx.Worker);
            Assert.AreEqual(cts, ctx.CancellationSource);
            Assert.AreEqual(cts.Token, ctx.Token);
        }

        [TestMethod]
        public void Token_ReturnsCancellationToken()
        {
            // Arrange
            var accepted = new AcceptedConnection(new Shared.Identity.ConnectionId("xyz"), _socket);
            var worker = new DummyWorker();
            var cts = new CancellationTokenSource();

            // Act
            var ctx = new ConnectionContext(accepted, worker, cts);

            // Assert
            Assert.AreEqual(cts.Token, ctx.Token);
        }
    }
}