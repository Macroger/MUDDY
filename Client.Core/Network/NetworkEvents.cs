namespace Client.Core.Network
{
    /// <summary>
    /// Event raised on the event bus to request a connection to the server.
    /// </summary>
    public sealed record ConnectRequestedEvent;

    /// <summary>
    /// Event raised on the event bus to request a disconnect from the server.
    /// </summary>
    public sealed record DisconnectRequestedEvent;
}
