using Shared.Identity;

namespace Shared.Domain.Player
{
    /// <summary>
    /// Represents the current state of a player, including connection information, name, location, and active conditions.
    /// </summary>
    /// <remarks>
    /// This class is typically used to track and manage player-specific data within a session or game world.
    /// </remarks>
    public class PlayerState
    {
        public required ConnectionId ConnId { get; init; }       // Required - every player state must have a connection ID.
        public required string PlayerName { get; init; }         // Required - every player state must have a name.
        public required RoomId CurrentLocation { get; init; }    // Required - every player state always has a location.
        public required IReadOnlySet<PlayerCondition> ActiveConditions { get; init; }  // Required - every player state must have an active conditions set, even if it's empty.

    }
}
