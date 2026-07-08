// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.Identity;
using Shared.Logging;
using Shared.Network.Transport;
using Shared.Network.Types;
using Shared.EventBus.EventTypes;

namespace Server.Core.Infrastructure.Events
{
    /// <summary>
    /// Contains events used to request sending a message to one or more clients.
    /// These events can be raised by system components outside the command pipeline. 
    /// Normally messages are only generated in response to player commands, but occasionally we need to send unsolicited messages.
    /// </summary>
    public class NetworkEvents
    {
        public class Commands
        {
            /// <summary>
            /// Raised when a system component needs to push a message to one or more connected clients.
            /// </summary>
            public sealed record SendMessageToClients(IReadOnlySet<ConnectionId> Recipients, PacketType MessageType, string Message) : BusEvent(EventMessageType.Network, LogLevel.Trace);

            /// <summary>
            /// Represents an event that signals the start of a listener operation.
            /// </summary>
            public sealed record StartListener() : BusEvent(EventMessageType.Network, LogLevel.Information);

            /// <summary>
            /// Represents an event that signals the stop of a listener operation.
            /// </summary>
            public sealed record StopListener() : BusEvent(EventMessageType.Network, LogLevel.Information);
        }

        /// <summary>
        /// This category is for lifecycle events such as server startup and shutdown. 
        /// </summary>
        public class Lifecycle
        {
            public sealed record NetworkSupervisorStarted(string Message) : BusEvent(EventMessageType.Network, LogLevel.Information);

            public sealed record NetworkSupervisorStopped(string Message) : BusEvent(EventMessageType.Network, LogLevel.Information);

            public sealed record AllClientsDisconnected(string Message) : BusEvent(EventMessageType.Network, LogLevel.Information);

            /// <summary>
            /// Represents an event that occurs when a client disconnects from the server.
            /// </summary>
            /// <param name="ConnId">The identifier of the connection that was disconnected.</param>
            public sealed record ClientDisconnected(ConnectionId ConnId, string Reason) : BusEvent(EventMessageType.Network, LogLevel.Information);

            /// <summary>
            /// Represents an event that occurs when a client connects to the server.
            /// </summary>
            /// <param name="ConnId"></param>
            /// <param name="Reason"></param>
            public sealed record ClientConnected(ConnectionId ConnId, string Reason) : BusEvent(EventMessageType.Network, LogLevel.Information);

            /// <summary>
            /// Represents an event that is raised when the state of a connection listener changes.
            /// </summary>
            public sealed record ListenerStateChanged(bool IsListenerStarted) : BusEvent(EventMessageType.Network, LogLevel.Information);
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
    }
}
