using Shared.EventBus.EventTypes;
using Shared.Logging;
using Shared.Network.Types;

namespace Client.Core.Infrastructure.Events
{
    public class MessageRouterEvents
    {
        public class Errors
        {
            /// <summary>
            /// Represents an event that is raised when an error occurs during network operations.
            /// </summary>
            /// <param name="ErrorMessage">A message describing the error that occurred.</param>
            /// <param name="Exception">The exception object associated with the error, if available.</param>
            public sealed record MessageRouterError(string ErrorMessage, Exception? Exception = null) : BusEvent(EventMessageType.CmdPipeline, LogLevel.Error);

            public sealed record HandlerRegistrationFailed(PacketType key, Exception? Exception = null) : BusEvent(EventMessageType.CmdPipeline, LogLevel.Error);
        }
    }
}
