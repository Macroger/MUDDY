using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.Player;
using Server.Core.Domain.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Domain.Services.Interfaces
{
    /// <summary>
    /// Queries world state and room information.
    /// </summary>
    public interface IWorldQueryService
    {
        /// <summary>
        /// Gets the full description of a room for a player.
        /// </summary>
        CommandResult LookAtRoom(
            PlayerState player,
            WorldState world);
    }
}
