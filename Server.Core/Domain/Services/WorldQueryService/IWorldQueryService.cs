// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.World;
using Shared.Domain.Player;

namespace Server.Core.Domain.Services.WorldQueryService
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
