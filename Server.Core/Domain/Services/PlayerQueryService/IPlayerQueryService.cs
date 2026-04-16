using Server.Core.CommandPipeline.Types;
using Shared.Domain.Player;
using Server.Core.Domain.World;

namespace Server.Core.Domain.Services.PlayerQueryService
{
    /// <summary>
    /// Queries player state and information.
    /// </summary>
    public interface IPlayerQueryService
    {
        /// <summary>
        /// Gets the player's status (name, location, conditions).
        /// </summary>
        CommandResult GetPlayerStatus(PlayerState player, WorldState world);

        /// <summary>
        /// Gets all players in the player's current room.
        /// </summary>
        CommandResult ListPlayersInRoom(PlayerState player, WorldState world);
    }
}
