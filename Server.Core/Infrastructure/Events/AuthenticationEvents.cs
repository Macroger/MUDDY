using Shared.EventBus.EventTypes;
using Shared.Identity;
using Shared.Logging;

namespace Server.Core.Infrastructure.Events
{
    public class AuthenticationEvents
    {
        public class Notifications
        {
            public sealed record AuthenticationAttemptEvent(string command) : BusEvent(EventMessageType.Chat, LogLevel.Debug);

            public sealed record AuthenticationSuccessEvent(string command) : BusEvent(EventMessageType.Chat, LogLevel.Debug);

            public sealed record SessionCreatedEvent(string message) : BusEvent(EventMessageType.Authentication, LogLevel.Debug);

            public sealed record SessionRemovedEvent(string message) : BusEvent(EventMessageType.Authentication, LogLevel.Debug);
        }        

        public class Errors
        {
            /// <summary>
            /// Represents an event that is raised when an error occurs during command pipeline operations.
            /// </summary>
            /// <param name="ErrorMessage">A message describing the error that occurred.</param>
            /// <param name="Exception">The exception object associated with the error, if available.</param>
            public sealed record AuthenticationError(string ErrorMessage, Exception? Exception = null) : BusEvent(EventMessageType.CmdPipeline, LogLevel.Error);

        }
    }
}
