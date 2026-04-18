using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.World;
using Shared.Domain.Player;

namespace Server.Core.Domain.Services.Interfaces
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
