// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Shared.Protocol.System
{
    /// <summary>
    /// Codes representing system-level responses from the server.
    ///
    /// These codes are used to communicate server lifecycle, session/authentication and protocol level
    /// conditions back to clients or internal components.
    /// </summary>
    public enum SystemResponseType
    {
        #region Server Lifecycle

        /// <summary>Server is undergoing maintenance and may be temporarily unavailable.</summary>
        ServerMaintenance,

        /// <summary>Server is in the process of shutting down and will not accept new requests.</summary>
        ServerShuttingDown,

        /// <summary>Server is temporarily unavailable due to overload or other transient issues.</summary>
        ServerUnavailable,

        #endregion
        #region Session and Authentication

        /// <summary>Client's session token is invalid or has been tampered with.</summary>
        InvalidSession,

        /// <summary>Client's session token has expired and needs to be renewed.</summary>
        SessionExpired,

        /// <summary>Client is not authenticated or does not have permission to perform the requested action.</summary>
        Unauthorized,

        #endregion
        #region Protocol and Transport

        /// <summary>Client sent a message that cannot be parsed or does not conform to the expected format.</summary>
        MalformedMessage,

        /// <summary>Client is using a protocol version that is not supported by the server.</summary>
        UnsupportedProtocolVersion,

        #endregion
        #region Abuse and Protection

        /// <summary>Client has exceeded the allowed rate of requests and should back off before retrying.</summary>
        RateLimited,

        #endregion
        #region Fall back for uncategorized errors - use sparingly

        /// <summary>An uncategorized or unexpected error occurred.</summary>
        UnknownError

        #endregion
    }
}
