using Shared.EventBus.EventTypes;
using Shared.Identity;
using Shared.Logging;

namespace Client.Core.Infrastructure.Events
{
    public class AuthenticationEvents
    {
        
        public class Notifications
        {            
            public sealed record SessionEstablished(SessionId id) : BusEvent(EventMessageType.Authentication, LogLevel.Debug);
        }
        public class Errors
        {
            /// <summary>
            /// Represents an event that is raised when an error occurs during authentication operations.
            /// </summary>
            /// <param name="ErrorMessage">A message describing the error that occurred.</param>
            /// <param name="Exception">The exception object associated with the error, if available.</param>
            public sealed record AuthenticationError(string ErrorMessage, Exception? Exception = null) : BusEvent(EventMessageType.Authentication, LogLevel.Error);
        }
    }
}
