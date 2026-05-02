// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.World;
using Shared.Domain.Player;

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
