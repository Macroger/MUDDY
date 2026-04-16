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

        /// <summary>
        /// Raised when a player is muted. This is a notification event, not a request.
        /// </summary>
        /// <param name="MutedPlayerName"></param>
        /// <param name="MutingEntityName"></param>
        /// <param name="Duration"></param>
        /// <param name="MutedPlayerConnections"></param>
        public sealed record PlayerMutedEvent(
            string MutedPlayerName,
            string MutingEntityName,
            TimeSpan Duration,
            IReadOnlySet<ConnectionId> MutedPlayerConnections);

        /// <summary>
        /// Raised when something wants to mute a player. This is a request event, not a notification.
        /// </summary>
        /// <param name="targetPlayerName"></param>
        public sealed record MutePlayerRequestEvent(string targetPlayerName);
    }
}
