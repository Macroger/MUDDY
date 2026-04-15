namespace Server.Core.Infrastructure.Lifecycle
{
    /// <summary>
    /// Provides data for an event that is raised when the server state changes.
    /// </summary>
    /// <remarks>This event data includes both the previous and new states of the server, allowing event
    /// handlers to determine the nature of the state transition.</remarks>
    public class ServerStateChangedEvent : EventArgs
    {
        /// <summary>
        /// Gets the previous state of the server before the most recent state change.
        /// </summary>
        public ServerStateEnum PreviousState { get; }
        
        /// <summary>
        /// Gets the new state of the server after a state change event.
        /// </summary>
        public ServerStateEnum NewState { get; }

        /// <summary>
        /// Initializes a new instance of the ServerStateChangedEventData class with the specified previous and new
        /// server states.
        /// </summary>
        /// <param name="previousState">The server state before the change occurred.</param>
        /// <param name="newState">The server state after the change occurred.</param>
        public ServerStateChangedEvent(ServerStateEnum previousState, ServerStateEnum newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }
}
