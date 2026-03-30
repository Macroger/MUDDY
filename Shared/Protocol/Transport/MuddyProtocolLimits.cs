namespace Shared.Protocol.Transport
{
    /// <summary>
    /// Defines server-side limits and constraints for the Muddy wire protocol.
    /// These values express what the server is willing to accept, not what
    /// the protocol format is.
    /// </summary>
    public sealed class MuddyProtocolLimits
    {
        public int MaxJsonBodyBytes { get; } = 16 * 1024;       // 16 KB - JSON body max size, to prevent abuse and ensure efficient processing
        public int MaxBinaryBodyBytes { get; } = 2 * 1024 * 1024; // 2 MB - Binary body max size, enough to allow for larger transfers like the 1 MB+ file upload scenario
        public int MaxPacketBytes { get; } = 2 * 1024 * 1024 + 64;  // 2 MB + header overhead - Total packet size limit, including headers and payload

        public int MaxJsonPacketBytes { get; } = 16 * 1024 + 64; // 16 KB + header overhead - Max total packet size for JSON payloads, ensuring the entire packet fits within reasonable limits

        public bool AllowBinaryPayloads { get; } = true;           // Whether the server accepts binary payloads at all, as an additional layer of control over protocol usage

        public int headerSize { get; } = 12;          // The size in bytes of the header.
        
        public int tailSize { get; } = 4;             // The size in bytes of the CRC value at the end of the packet.
    }
}
