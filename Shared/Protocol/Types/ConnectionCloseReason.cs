namespace Shared.Protocol.Types
{
    public enum ConnectionCloseReason
    {
        // Normal lifecycle
        Normal = 0,
        ClientDisconnected,

        // Server state
        ServerShutdown,
        MaintenanceMode,
        AdministrativeKick,

        // Timeouts
        IdleTimeout,

        // Protocol / behavior
        ProtocolViolation,
        MalformedMessage,
        MessageSizeExceeded,

        // Security
        AuthenticationFailed,
        AuthorizationDenied,

        // Errors
        TransportError,
        WorkerFaulted,
        InternalServerError
    }
}
