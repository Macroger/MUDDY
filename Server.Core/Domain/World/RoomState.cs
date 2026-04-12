using Server.Core.CommandPipeline.ContextBuilder;
using Shared.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Domain.World
{
    public class RoomState
    {
        public string RoomId { get; init; }         // Required - every room must have a unique identifier.
        public string? Description { get; init; }    // Optional - some rooms may have a description, while others may not.
        
        /// <summary>
        /// Gets the set of conditions currently applied to the room.
        /// </summary>
        /// <remarks>The set is read-only and reflects all active conditions. Modifying the returned set
        /// will result in a runtime exception. Conditions may affect room behavior or state depending on their
        /// type.</remarks>
        public IReadOnlySet<RoomCondition> Conditions { get; init; } = new HashSet<RoomCondition>();

        /// <summary>
        /// Gets the set of player connection IDs currently present in the room.
        /// </summary>
        public IReadOnlySet<ConnectionId> PlayersPresent { get; init; } = new HashSet<ConnectionId>();

        public RoomState(
            string roomId,
            string? description,
            IReadOnlySet<RoomCondition> roomConditions,
            IReadOnlySet<ConnectionId> playersInRoom)
        {
            //ISSUE - I DON"T LIKE ROOMID BEING A STRING - MAKE INTO A TYPE OF ITS OWN
        }
    }
}
