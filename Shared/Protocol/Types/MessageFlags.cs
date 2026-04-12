namespace Shared.Protocol.Types
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
        SystemMessage = 1 << 3
    }
}
