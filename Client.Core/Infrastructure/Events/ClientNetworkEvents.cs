// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.EventBus.EventTypes;
using Shared.Logging;
using Shared.Network.Transport;

namespace Client.Core.Infrastructure.Events
{
    public class ClientNetworkEvents
    {
        public class Commands
        {
            /// <summary>
            /// Event raised on the event bus to request a connection to the server.
            /// </summary>
            public sealed record ConnectToServer(string serverAddress, int serverPort) : BusEvent(EventMessageType.Network, LogLevel.Information);

            /// <summary>
            /// Event raised on the event bus to request a disconnect from the server.
            /// </summary>
            public sealed record DisconnectFromServer() : BusEvent(EventMessageType.Network, LogLevel.Information);

            /// <summary>
            /// Event raised on the event bus to request sending a message to the server.
            /// </summary>
            /// <param name="message">The message to send. </param>
            public sealed record SendMessageToServer(string message) : BusEvent(EventMessageType.Network, LogLevel.Information);
        }
        public class Errors
        {
            /// <summary>
            /// Represents an event that is raised when an error occurs during network operations.
            /// </summary>
            /// <param name="ErrorMessage">A message describing the error that occurred.</param>
            /// <param name="Exception">The exception object associated with the error, if available.</param>
            public sealed record NetworkError(string ErrorMessage, Exception? Exception = null) : BusEvent(EventMessageType.Network, LogLevel.Error);

        }
        public class Packets
        {
            /// <summary>
            /// An event that is raised when a message is sent to a client. 
            /// This can be used for logging, monitoring, or triggering other actions.
            /// </summary>
            public sealed record PacketSent(PacketEnvelope envelope) : BusEvent(EventMessageType.Network, LogLevel.Trace);

            public sealed record PacketReceived(PacketEnvelope envelope) : BusEvent(EventMessageType.Network, LogLevel.Trace);
        }

        public class Lifecycle
        {
            /// <summary>
            /// An event that is raised when the connection status changes (e.g., connected or disconnected).
            /// This can be used to update the UI or trigger other actions based on the connection state.
            /// </summary>
            public sealed record ConnectionStatusChangedEvent(bool ConnectionStatus, string Message) : BusEvent(EventMessageType.Network, LogLevel.Information);

            public sealed record SupervisorShutdown(string message) : BusEvent(EventMessageType.Network, LogLevel.Information);
        }
    }    

}
