using Shared.Identity;
using Shared.Protocol.Transport;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.EventBus
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
    }
}
