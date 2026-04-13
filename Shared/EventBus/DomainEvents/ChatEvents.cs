using Shared.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.EventBus.DomainEvents
{
    public class ChatEvents
    {
        /// <summary>
        /// Raised when a player says something in a room.
        /// </summary>
        public sealed record PlayerSaidEvent(
            string SenderName,
            RoomId RoomId,
            string Message,
            IReadOnlySet<ConnectionId> PlayersInRoom);

        /// <summary>
        /// Raised when a player emotes.
        /// </summary>
        public sealed record PlayerEmotedEvent(
            string SenderName,
            RoomId RoomId,
            string EmoteText,
            IReadOnlySet<ConnectionId> PlayersInRoom);
    }
}
