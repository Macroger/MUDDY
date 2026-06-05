using Shared.Logging;

namespace Shared.EventBus.EventTypes
{
    public class CmdPipelineEvents
    {
        public class Commands
        {

        }

        public class Lifecycle
        {

        }

        public class Errors
        {
            /// <summary>
            /// Represents an event that is raised when an error occurs during command pipeline operations.
            /// </summary>
            /// <param name="ErrorMessage">A message describing the error that occurred.</param>
            /// <param name="Exception">The exception object associated with the error, if available.</param>
            public sealed record CmdPipeLineError(string ErrorMessage, Exception? Exception = null) : BusEvent(EventMessageType.CmdPipeline, LogLevel.Error);

        }
    }
}
