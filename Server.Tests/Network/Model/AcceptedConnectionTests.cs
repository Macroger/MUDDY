using Shared.Identity;
using System.Net;
using System.Net.Sockets;

namespace Server.Core.Network.Model.Tests
{
    [TestClass]
    public class AcceptedConnectionTests
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

        [TestMethod]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var connId = new ConnectionId(Guid.NewGuid().ToString());

            // Act
            var accepted = new AcceptedConnection(connId, _socket);

            // Assert
            Assert.AreEqual(connId, accepted.connId);
            Assert.AreEqual(_socket, accepted.clientSocket);
            Assert.AreEqual(_socket.RemoteEndPoint, accepted.RemoteEndPoint);
            Assert.AreEqual(_socket.LocalEndPoint, accepted.LocalEndPoint);
            Assert.IsLessThan(2, (DateTime.UtcNow - accepted.AcceptedAtUtc).TotalSeconds);
            Assert.IsNotNull(accepted.Lifetime);
        }

        [TestMethod]
        public void Lifetime_IsNewCancellationTokenSource()
        {
            // Arrange
            var connId = new ConnectionId(Guid.NewGuid().ToString());
            var accepted = new AcceptedConnection(connId, _socket);

            // Act
            accepted.Lifetime.Cancel();

            // Assert
            Assert.IsTrue(accepted.Lifetime.IsCancellationRequested);
        }

        [TestMethod]
        public void RemoteAndLocalEndPoint_MatchSocket()
        {
            // Arrange
            var connId = new ConnectionId(Guid.NewGuid().ToString());
            var accepted = new AcceptedConnection(connId, _socket);

            // Assert
            Assert.AreEqual(_socket.RemoteEndPoint, accepted.RemoteEndPoint);
            Assert.AreEqual(_socket.LocalEndPoint, accepted.LocalEndPoint);
        }

        [TestMethod]
        public void Constructor_Throws_WhenSocketIsNull()
        {
            // Arrange + Act + Assert
            Assert.Throws<ArgumentNullException>(() => new AcceptedConnection(new Shared.Identity.ConnectionId("abc"), null!));
        }

    }
}