using Client.Core.State.Player;
using Shared.EventBus.EventTypes;
using Shared.Logging;

namespace Client.Core.Infrastructure.Events
{
    public class PlayerStateEvents
    {        
        public class Notifications
        {   
            public sealed record PlayerStateUpdate(PlayerState updatedPlayerState) : BusEvent(EventMessageType.Player, LogLevel.Debug);
        }
        public class Errors
        {
            /// <summary>
            /// Represents an event that is raised when an error occurs during authentication operations.
            /// </summary>
            /// <param name="ErrorMessage">A message describing the error that occurred.</param>
            /// <param name="Exception">The exception object associated with the error, if available.</param>
            public sealed record PlayerStateError(string ErrorMessage, Exception? Exception = null) : BusEvent(EventMessageType.Player, LogLevel.Error);
        }
    }
}
