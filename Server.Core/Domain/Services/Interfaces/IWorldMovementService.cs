using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.Player;
using Server.Core.Domain.World;
using Server.Core.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Domain.Services.Interfaces
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
