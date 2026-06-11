namespace Shared.Network.Types
{
    public enum PacketType
    {
        AuthSuccess,        // Server -> client authentication success message
        BinaryTransfer,     // Server -> client raw binary payload (e.g. image data)
        Chat,               // client <-> client chat message
        Command,            // Client -> server request
        Error,              // Error response (protocol or server-side)
        Event,              // Server -> client unsolicited event
        Ping,               // Server <-> client heartbeat
        Response,           // Server -> client reply to a command
        System              // Infrastructure / system-level message
    }
}
