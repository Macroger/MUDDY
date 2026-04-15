using Shared.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.EventBus.DomainEvents
{
    public class PlayerEvents
    {
        /// <summary>
        /// Published by <see cref="InMemoryPlayerRepository"/> after a player's state has been
        /// removed. Subscribers use the <see cref="LastRoom"/> to clean up their own state.
        /// </summary>
        /// <param name="ConnId">The connection that disconnected.</param>
        /// <param name="LastRoom">The room the player was in when they left.</param>
        public sealed record PlayerLeftWorldEvent(ConnectionId ConnId, RoomId LastRoom);
    }
}
