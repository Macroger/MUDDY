using Server.Core.CommandPipeline.ContextBuilder;
using Shared.Identity;

namespace Server.Core.Domain.Player
{
    /// <summary>
    /// Represents the current state of a player, including connection information, name, location, and active conditions.
    /// </summary>
    /// <remarks>
    /// This class is typically used to track and manage player-specific data within a session or game world.
    /// </remarks>
    public class PlayerState
    {
        public required ConnectionId ConnId { get; init; }       // Required - every player must have a connection ID.
        public required string PlayerName { get; init; }         // Required - every player must have a name.
        public required RoomId CurrentLocation { get; init; }    // Required - every player always has a location.

        public required IReadOnlySet<PlayerCondition> ActiveConditions { get; init; }  // Required - every player must have an active conditions set, even if it's empty.

        public PlayerState(ConnectionId connection, string playerName, RoomId playerLocation, IReadOnlySet<PlayerCondition>? playerConditions)
        {
            ConnId = connection;
            PlayerName = playerName;
            CurrentLocation = playerLocation;
            ActiveConditions = playerConditions ?? new HashSet<PlayerCondition>();
        }
    }
}
