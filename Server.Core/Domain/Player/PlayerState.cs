using Server.Core.CommandPipeline.ContextBuilder;
using Shared.Identity;

namespace Server.Core.Domain.Player
{
    /// <summary>
    /// Represents the current state of a player, including connection information, name, location, and active
    /// conditions.
    /// </summary>
    /// <remarks>This class is typically used to track and manage player-specific data within a session or
    /// game world. All properties are immutable after initialization, ensuring thread safety and consistency when
    /// accessed concurrently.</remarks>
    public class PlayerState
    {
        public ConnectionId ConnId { get; init; }       // Required - every player must have a connection ID.
        public string? PlayerName { get; init; }        // Allow null for players who haven't set a name yet.
        public RoomId CurrentLocation { get; init; }    // Required - every player always has a location.

        /// <summary>
        /// Gets the set of conditions that are currently active for the player.
        /// </summary>
        /// <remarks>The set is read-only and reflects the current state of all active conditions.
        /// Conditions may affect gameplay or player abilities depending on their type.</remarks>
        public IReadOnlySet<PlayerCondition> ActiveConditions { get; init; } = new HashSet<PlayerCondition>();

        public PlayerState(ConnectionId connection, string? playerName, RoomId playerLocation)
        {
            ConnId = connection;
            PlayerName = playerName;
            CurrentLocation = playerLocation;
            ActiveConditions = new HashSet<PlayerCondition>();
        }
    }
}
