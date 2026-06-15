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

        public class Notifications
        {
            public sealed record ReceivedAuthenticationMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedBinaryTransferMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedChatMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedErrorMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedEventMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedResponseMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedSystemMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            
        }
    }
}