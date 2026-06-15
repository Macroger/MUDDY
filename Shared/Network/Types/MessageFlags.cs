// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Shared.Network.Types
{
    /// <summary>
    /// Bit flags that describe metadata and processing requirements for protocol messages.
    /// </summary>
    [Flags]
    public enum MessageFlags
    {
        /// <summary>No special flags.</summary>
        None = 0,

        /// <summary>Message requires the sender to be authenticated.</summary>
        RequiresAuthentication = 1 << 0,

        /// <summary>Message payload is encrypted.</summary>
        Encrypted = 1 << 1,

        /// <summary>Message payload is compressed.</summary>
        Compressed = 1 << 2,

        /// <summary>Message is a system-level message rather than an application-level message.</summary>
        SystemMessage = 1 << 3,

        /// <summary>Message payload is raw binary data (e.g. a JPEG image). Relaxes the JSON body size cap.</summary>
        BinaryPayload = 1 << 4
    }
}
