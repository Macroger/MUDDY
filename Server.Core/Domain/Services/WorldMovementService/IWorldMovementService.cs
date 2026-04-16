using Server.Core.CommandPipeline.Types;
using Shared.Domain.Player;
using Server.Core.Domain.World;

namespace Server.Core.Domain.Services.WorldMovementService
{
    /// <summary>
    /// Manages player movement within the game world.
    /// </summary>
    public interface IWorldMovementService
    {
        /// <summary>
        /// Moves a player in the specified direction.
        /// </summary>
        Task<CommandResult> MovePlayerAsync(
            PlayerState player,
            WorldState world,
            string direction
            );
    }
}
