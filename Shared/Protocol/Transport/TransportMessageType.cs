namespace Shared.Protocol.Transport
{
    public enum TransportMessageType
    {
        Command = 0,   // Client → server request
        Response,      // Server → client reply to a command
        Event,         // Server → client unsolicited event
        Chat,          // client → client chat message
        Error,         // Error response (protocol or server-side)
        Ping,          // Server -> client heartbeat
        System         // Infrastructure / system-level message
    }
}
