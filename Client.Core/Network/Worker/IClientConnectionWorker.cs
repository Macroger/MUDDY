// =============================================================================
/// @file       IClientConnectionWorker.cs
/// @namespace  Client.Core.Network.Worker
/// @brief      Infrastructure interface for client connection worker.
///             Defines contract for single client-to-server TCP connection.
/// @details    Uses C# events for internal infrastructure
///             signaling. Supervisor translates these to domain events on the 
///             main eventBus.
// =============================================================================

using Shared.Network.Transport;

namespace Client.Core.Network.Worker
{
    /// <summary>
    /// Defines the contract for a client connection worker that manages
    /// a single TCP connection to the server.
    /// 
    /// INFRASTRUCTURE EVENTS: This interface uses C# events because the worker and
    /// supervisor are lifecycle-coupled. The supervisor translates these
    /// infrastructure signals to domain events via the event bus.
    /// </summary>
    public interface IClientConnectionWorker
    {
        /// <summary>
        /// Gets a value indicating whether the worker is currently connected and running.
        /// </summary>
        public bool IsRunning { get; }

        /// <summary>
        /// Starts the worker and establishes the TCP connection.
        /// Returns <see langword="false"/> if already running; throws on connection failure.
        /// </summary>
        bool Start();

        /// <summary>
        /// Stops the worker and closes the connection gracefully.
        /// </summary>
        void Stop();

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="envelope">The transport envelope to send.</param>
        /// <returns>True if the message was accepted for sending; false if worker is not running.</returns>
        bool SendEnvelope(PacketEnvelope envelope);

        /// <summary>
        /// INFRASTRUCTURE EVENT: Raised when a complete packet is received from the server.
        /// Supervisor subscribes and translates to domain event via event bus.
        /// </summary>
        event EventHandler<PacketEnvelope>? PacketReceived;

        /// <summary>
        /// INFRASTRUCTURE EVENT: Raised when the connection is closed or worker shuts down.
        /// Supervisor subscribes and translates to domain event via event bus.
        /// </summary>
        event EventHandler? ConnectionClosed;

        /// <summary>
        /// INFRASTRUCTURE EVENT: Raised when an error occurs in the worker.
        /// Supervisor subscribes and translates to domain event via event bus.
        /// </summary>
        event EventHandler<Exception>? ErrorOccurred;

        /// <summary>
        /// INFRASTRUCTURE EVENT: Raised after a packet is successfully sent to the server.
        /// Supervisor subscribes and translates to domain event via event bus.
        /// </summary>
        event EventHandler<PacketEnvelope>? PacketSent;
    }
}