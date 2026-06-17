namespace Shared.Network.Types
{
    public enum PacketType
    {
        Authentication,     // Authentication related messages
        BinaryTransfer,     // Raw binary payload messages (e.g. image data)
        Command,            // Request message - command or query
        Error,              // Error response (protocol or server-side)
        Event,              // Server -> client unsolicited event
        Ping,               // Server <-> client heartbeat
        Response,           // Server -> client reply to a command
        System              // Infrastructure / system-level message
    }
}
