namespace Server.Core.Infrastructure.Lifecycle
{
    /// <summary>
    /// Event published to request a server state change (e.g., from the GUI).
    /// </summary>
    public class ServerStateChangeRequestedEvent
    {
        public ServerStateEnum RequestedState { get; }

        public ServerStateChangeRequestedEvent(ServerStateEnum requestedState)
        {
            RequestedState = requestedState;
        }
    }
}
