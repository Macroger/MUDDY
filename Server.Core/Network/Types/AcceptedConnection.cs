using System.Net;
using System.Net.Sockets;
using System.Threading;
using Shared.Identity;

namespace Server.Core.Network.Model
{
    /// <summary>
    /// Represents a TCP connection that has been accepted by a listener, including connection metadata and lifetime
    /// management.
    /// </summary>
    /// <remarks>This class provides access to the accepted socket, connection endpoints, and a cancellation
    /// token source for managing the connection's lifetime. Instances are typically created by server components when a
    /// new client connection is established. The class is sealed to prevent inheritance.</remarks>
    public sealed class AcceptedConnection
    {
        public ConnectionId connId { get; }
        public Socket clientSocket { get; }

        public EndPoint? RemoteEndPoint { get; }
        public EndPoint? LocalEndPoint { get; }

        public DateTime AcceptedAtUtc { get; }
        public CancellationTokenSource Lifetime { get; }

        public AcceptedConnection(
            ConnectionId id,
            Socket socket)
        {

            if (socket == null) throw new ArgumentNullException(nameof(socket), "Socket cannot be null when creating an AcceptedConnection.");

            connId = id;
            clientSocket = socket;

            RemoteEndPoint = socket.RemoteEndPoint;
            LocalEndPoint = socket.LocalEndPoint;

            AcceptedAtUtc = DateTime.UtcNow;
            Lifetime = new CancellationTokenSource();
        }
    }
}
