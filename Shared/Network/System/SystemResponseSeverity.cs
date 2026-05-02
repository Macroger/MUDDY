// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Shared.Protocol.System
{
    /// <summary>
    /// Severity levels for system responses.
    /// </summary>
    public enum SystemResponseSeverity
    {
        /// <summary>Informational messages that do not indicate issues.</summary>
        Info,

        /// <summary>Potential issues that clients should be aware of but may not require immediate action.</summary>
        Warning,

        /// <summary>Problems that may require attention or action from the client.</summary>
        Error,

        /// <summary>Critical failures requiring immediate attention; server may be unstable.</summary>
        Fatal
    }
}
