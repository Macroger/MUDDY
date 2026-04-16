namespace Shared.Protocol.Types
{
    /// <summary>
    /// Reasons why a connection may be closed.
    /// </summary>
    public enum ConnectionCloseReason
    {
        // Normal lifecycle
        /// <summary>Normal closure initiated by client or server.</summary>
        Normal = 0,
        /// <summary>Client disconnected unexpectedly.</summary>
        ClientDisconnected,

        // Server state
        /// <summary>Server is shutting down and closing connections.</summary>
        ServerShutdown,
        /// <summary>Server is in maintenance mode and closing connections.</summary>
        MaintenanceMode,
        /// <summary>Administrative action kicked the client from the server.</summary>
        AdministrativeKick,

        // Timeouts
        /// <summary>Connection closed due to idle timeout.</summary>
        IdleTimeout,

        // Protocol / behavior
        /// <summary>Connection closed due to protocol violations.</summary>
        ProtocolViolation,
        /// <summary>Connection closed because a malformed message was received.</summary>
        MalformedMessage,
        /// <summary>Connection closed because a message exceeded allowed size.</summary>
        MessageSizeExceeded,

        // Security
        /// <summary>Authentication failed for the client.</summary>
        AuthenticationFailed,
        /// <summary>Client was denied authorization for the attempted action.</summary>
        AuthorizationDenied,

        // Errors
        /// <summary>Transport-level error occurred (socket/IO).</summary>
        TransportError,
        /// <summary>Connection worker faulted unexpectedly.</summary>
        WorkerFaulted,
        /// <summary>Internal server error led to connection termination.</summary>
        InternalServerError
    }
}
