// =============================================================================
/// @file       ClientGuiEvents.cs
/// @namespace  Client.Core.Infrastructure.Events
/// @brief      GUI-specific events.
// =============================================================================
using Shared.EventBus.EventTypes;
using Shared.Logging;
using Shared.Network.Transport;

namespace Client.Core.Infrastructure.Events
{
    public class ClientGuiEvents
    {

        public class ConnectionMonitor
        {
        }

        public class Commands
        {
            /// <summary>
            /// Published when the GUI wants to send a message to the server (e.g., from a chat input or command line).
            /// </summary>
            /// <param name="Message">The message the GUI wants to send to the server.</param>
            public sealed record SendMessageToServer(string Message) : BusEvent(EventMessageType.Gui, LogLevel.Information);
        }

        public class Errors
        {
            /// <summary>
            /// Represents an event that is raised when an error occurs during GUI operations.
            /// </summary>
            /// <param name="ErrorMessage">A message describing the error that occurred.</param>
            /// <param name="Exception">The exception object associated with the error, if available.</param>
            public sealed record GuiError(string ErrorMessage, Exception? Exception = null) : BusEvent(EventMessageType.Gui, LogLevel.Error);
        }

        public class Notifications
        {
            public sealed record ReceivedAuthenticationMessage(MessageEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedBinaryTransferMessage(MessageEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedErrorMessage(MessageEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedEventMessage(MessageEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedResponseMessage(MessageEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedSystemMessage(MessageEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);
        }
    }
}