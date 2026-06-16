using Shared.Logging;
using Server.Core.Infrastructure.Lifecycle;
using Shared.EventBus.EventTypes;

namespace Server.Core.Infrastructure.Events
{
    public class SystemEvents
    {
        public class Commands
        {
            public record ServerStateChangeRequest(ServerStateEnum previousState, ServerStateEnum newState) : BusEvent(EventMessageType.System, LogLevel.Information);
            public record CommandExecutedEvent(string CommandText, string PlayerId) : BusEvent(EventMessageType.System, LogLevel.Information);
        }
        public class Errors
        {
            public record SystemErrorEvent(string ErrorMessage, Exception? Exception = null) : BusEvent(EventMessageType.System, LogLevel.Error);
        }

        public class Diagnostics
        {
        }

        public class Performance
        {            
        }

        public class Lifecycle
        {
            public record ServerStateChangedEvent(ServerStateEnum PreviousState, ServerStateEnum NewState) : BusEvent(EventMessageType.System, LogLevel.Information);
        }

    }
}
