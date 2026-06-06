// =============================================================================
/// @file       ServerGuiEvents.cs
/// @namespace  Server.GUI.Events
/// @brief      GUI-specific events for the server administrative interface.
/// @details    These events are published and subscribed exclusively within
///             Server.GUI. They drive UI updates via DispatcherQueue and are
///             not visible to the client or shared libraries.
// =============================================================================
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Logging;

namespace Client.Core.Events
{
    public class ClientGuiEvents
    {
        public class Console
        {
            /// <summary>
            /// Published when the client console should display a new log line.
            /// </summary>
            public record ConsoleOutputEvent(string Message, LogLevel Severity)
                : BusEvent(EventMessageType.Gui, LogLevel.Debug);
        }

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
            ///// <summary>
            ///// Published when the "Start Server" button is clicked.
            ///// </summary>
            //public record StartServerRequestedEvent()
            //    : BusEvent(EventMessageType.Gui, LogLevel.Information);

            ///// <summary>
            ///// Published when the "Stop Server" button is clicked.
            ///// </summary>
            //public record StopServerRequestedEvent()
            //    : BusEvent(EventMessageType.Gui, LogLevel.Information);
        }

        public class Notifications
        {
            /// <summary>
            /// Published when the GUI should show a toast/notification.
            /// </summary>
            public record ShowNotificationEvent(string Title, string Message, NotificationSeverity Severity)
                : BusEvent(EventMessageType.Gui, LogLevel.Debug);
        }
    }

    public enum NotificationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }
}