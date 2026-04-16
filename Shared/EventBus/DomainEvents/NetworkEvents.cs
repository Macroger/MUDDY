using Shared.Identity;
using Shared.Protocol.Transport;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.EventBus.DomainEvents
{
    /// <summary>
    /// Contains events used to request sending a message to one or more clients.
    /// These events can be raised by system components outside the command pipeline. 
    /// Normally messages are only generated in response to player commands, but occasionally we need to send unsolicited messages.
    /// </summary>
    public class NetworkEvents
    {
        /// <summary>
        /// Raised when any system component needs to push a message to one or more
        /// connected clients without going through the command pipeline.
        /// </summary>
        public sealed record OutboundMessageEvent(
            IReadOnlySet<ConnectionId> Recipients,
            TransportMessageType MessageType,
            string Message
        );
        /// <summary>
        /// Represents an event that occurs when a client disconnects from the server.
        /// </summary>
        /// <param name="ConnId">The identifier of the connection that was disconnected.</param>
        public sealed record ClientDisconnectedEvent(ConnectionId ConnId);

        /// <summary>
        /// Represents an event that is raised when the state of a listener changes.
        /// </summary>
        public sealed record ListenerStateChangedEvent(bool IsListenerStarted);

        /// <summary>
        /// Represents an event that signals the start of a listener operation.
        /// </summary>
        public sealed record StartListnerRequestEvent();

        /// <summary>
        /// Represents an event that signals the stop of a listener operation.
        /// </summary>
        public sealed record StopListenerRequestEvent();
    }
}
