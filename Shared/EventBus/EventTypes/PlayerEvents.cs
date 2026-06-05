// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.Identity;
using Shared.Logging;

namespace Shared.EventBus.EventTypes
{
    public class PlayerEvents
    {
        /// <summary>
        /// Published by <see cref="InMemoryPlayerRepository"/> after a player's state has been
        /// removed. Subscribers use the <see cref="LastRoom"/> to clean up their own state.
        /// </summary>
        /// <param name="ConnId">The connection that disconnected.</param>
        /// <param name="PlayerName">The name of the player who left.</param>
        /// <param name="LastRoom">The room the player was in when they left.</param>
        public sealed record PlayerLeftWorldEvent(ConnectionId ConnId, string PlayerName, RoomId LastRoom) : BusEvent(EventMessageType.Player, LogLevel.Information);

        /// <summary>
        /// Represents an event that occurs when a player enters the world, including connection and starting location
        /// details.
        /// </summary>
        /// <param name="ConnId">The unique identifier for the player's connection associated with this event.</param>
        /// <param name="PlayerName">The name of the player who has entered the world. Cannot be null or empty.</param>
        /// <param name="StartingRoom">The identifier of the room where the player starts upon entering the world.</param>
        public sealed record PlayerEnteredWorldEvent(ConnectionId ConnId, string PlayerName, RoomId StartingRoom) : BusEvent(EventMessageType.Player, LogLevel.Information);

        //public sealed record PlayerStateUpdatedEvent(ConnectionId ConnId, PlayerState );

    }
}
