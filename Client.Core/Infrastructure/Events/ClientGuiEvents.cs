// =============================================================================
/// @file       ServerGuiEvents.cs
/// @namespace  Server.GUI.Events
/// @brief      GUI-specific events for the server administrative interface.
/// @details    These events are published and subscribed exclusively within
///             Server.GUI. They drive UI updates via DispatcherQueue and are
///             not visible to the client or shared libraries.
// =============================================================================
using Shared.EventBus.EventTypes;
using Shared.Logging;
using Shared.Network.Transport;

namespace Client.Core.Infrastructure.Events
{
    public class ClientGuiEvents
    {

        //public class ConnectionMonitor
        //{
        //    /// <summary>
        //    /// Published when the connection count changes (for status bar updates).
        //    /// </summary>
        //    public record ConnectionCountChangedEvent(int ActiveConnections)
        //        : BusEvent(EventMessageType.Gui, LogLevel.Debug);
        //}

        public class Commands
        {

        }

        public class Notifications
        {
            /// <summary>
            /// Published when the GUI should show a toast/notification.
            /// </summary>
            //public record ShowNotificationEvent(string Title, string Message, NotificationSeverity Severity) : BusEvent(EventMessageType.Gui, LogLevel.Debug);

            public sealed record ReceivedAuthenticationMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedBinaryTransferMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedChatMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedErrorMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedEventMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedResponseMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            public sealed record ReceivedSystemMessage(PacketEnvelope envelope) : BusEvent(EventMessageType.Gui, LogLevel.Information);

            
        }
    }

    public enum NotificationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }
}