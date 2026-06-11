// =============================================================================
/// @file       IClientNetworkSupervisor.cs
/// @namespace  Client.Core.Network.Supervisor
/// @brief      Interface for client network supervisor managing single connection to server.
///             Specializes the supervisor pattern for client context (one server connection).
// =============================================================================

using Shared.Network.Transport;

namespace Client.Core.Network.Supervisor
{
    /// <summary>
    /// Defines the contract for a client network supervisor that manages
    /// the lifecycle of a single connection to a server.
    /// </summary>
    public interface IClientNetworkSupervisor : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the supervisor is currently connected to the server.
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Establishes a connection to the server and starts the worker.
        /// </summary>
        /// <param name="serverAddress">The server address to connect to.</param>
        /// <param name="serverPort">The server port to connect to.</param>
        /// <returns>True if connection was successful; otherwise, false.</returns>
        Task<bool> StartConnectionAsync(string serverAddress, int serverPort);

        /// <summary>
        /// Closes the connection to the server and stops the worker.
        /// </summary>
        /// <returns>True if disconnection was successful; otherwise, false.</returns>
        bool StopConnection();

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="envelope">The transport envelope to send.</param>
        /// <returns>True if the message was accepted for sending; otherwise, false.</returns>
        bool SendEnvelopeToServer(PacketEnvelope envelope);       

        /// <summary>
        /// Performs graceful shutdown of the supervisor and all resources.
        /// </summary>
        void ShutdownSupervisor();
    }
}