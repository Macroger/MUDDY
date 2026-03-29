using System;
using System.Collections.Generic;
using System.Text;

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
        AuthenticationTimeout,

        // Protocol / behavior
        ProtocolViolation,
        MalformedMessage,
        MessageSizeExceeded,
        UnsupportedProtocolVersion,

        // Security
        AuthenticationFailed,
        AuthorizationDenied,
        RateLimitExceeded,

        // Errors
        TransportError,
        WorkerFaulted,
        InternalServerError
    }
}
