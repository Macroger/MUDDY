// =============================================================================
/// @file       EstablishedConnection.cs
/// @namespace  Client.Core.Network.Types
/// @brief      DTO representing an established client-to-server TCP connection.
///             Mirrors the server's AcceptedConnection pattern.
// =============================================================================

using System.Net;
using System.Net.Sockets;

namespace Client.Core.Network.Types
{
    /// <summary>
    /// Encapsulates the details of an established connection from client to server.
    /// Contains the TCP client, network stream, and remote endpoint information.
    /// </summary>
    public sealed class EstablishedConnection
    {
        /// <summary>
        /// Gets the TcpClient managing the connection to the server.
        /// </summary>
        public TcpClient TcpClient { get; init; } = null!;

        /// <summary>
        /// Gets the network stream for reading and writing data.
        /// </summary>
        public NetworkStream NetworkStream { get; init; } = null!;

        /// <summary>
        /// Gets the remote endpoint (server address and port).
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; init; } = null!;
    }
}