using Shared.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.EventBus.EventTypes
{
    public class SystemEvents
    {
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
