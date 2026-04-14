using Server.Core.CommandPipeline.ContextBuilder;
using Shared.Identity;

namespace Server.Core.Domain.World
{
    public class RoomState
    {
        public RoomId Id { get; init; }             // Required - every room must have a unique identifier.
        public string? Description { get; init; }   // Optional - some rooms may have a description, while others may not.
        
        /// <summary>
        /// Gets the set of conditions currently applied to the room.
        /// </summary>
        /// <remarks>The set is read-only and reflects all active conditions.
        /// Conditions may affect room behavior or state depending on their type.
        /// </remarks>
        public IReadOnlySet<RoomCondition> Conditions { get; init; } = new HashSet<RoomCondition>();

        /// <summary>
        /// Gets the set of player connection IDs currently present in the room.
        /// </summary>
        public IReadOnlySet<ConnectionId> PlayersPresent { get; init; } = new HashSet<ConnectionId>();

        /// <summary>
        /// Maps direction names (e.g. "north") to the destination <see cref="RoomId"/>.
        /// If a direction is not in this dictionary, there is no exit in that direction.
        /// </summary>
        public IReadOnlyDictionary<string, RoomId> Exits { get; init; } = new Dictionary<string, RoomId>();

        public RoomState(
            RoomId id,
            string? description,
            IReadOnlySet<RoomCondition> roomConditions,
            IReadOnlySet<ConnectionId> playersInRoom,
            IReadOnlyDictionary<string, RoomId>? exits = null)
        {
            Id = id;
            Description = description ?? "";
            Conditions = roomConditions ?? new HashSet<RoomCondition>();
            PlayersPresent = playersInRoom ?? new HashSet<ConnectionId>();
            Exits = exits ?? new Dictionary<string, RoomId>();
        }
    }
}
