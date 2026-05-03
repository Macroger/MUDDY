// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.Services.Interfaces;
using Server.Core.Domain.World;
using Shared.Domain.Player;

namespace Server.Core.Domain.Services.ConcreteClasses
{
    public class PlayerQueryService : IPlayerQueryService
    {
        /// <summary>
        /// Retrieves the current status of the specified player, including name, location, and active conditions.
        /// </summary>
        /// <param name="player">The player whose status information is to be retrieved. Cannot be null.</param>
        /// <param name="world">The current world state, used to provide context for the player's status. Cannot be null.</param>
        /// <returns>A CommandResult containing a formatted message with the player's name, location, and active conditions.</returns>
        public CommandResult GetPlayerStatus(PlayerState player, WorldState world)
        {
            // Get the players details
            var playerName = player.PlayerName;
            var playerLocation = player.CurrentLocation.ToString();
            var playerConditions = string.Join(", ", player.ActiveConditions);

            // Generate a command result message containing the details
            CommandResult response = new CommandResult
            {
                Success = true,
                Message = $"Player: {playerName}\nLocation: {playerLocation}\nConditions: {playerConditions}"
            };

            return response;
        }

        /// <summary>
        /// Lists the players in the same room as the specified player, providing a message with the names of those players.
        /// </summary>
        /// <param name="player"> The player for whom to list the other players in the same room. Cannot be null.</param>
        /// <param name="world"> The current world state, used to determine the players present in the same room. Cannot be null.</param>
        /// <returns type="CommandResult"> Returns a CommandResult containing a message with the names of the players in the same room as the specified player.
        /// If there are no other players, the message will indicate that as well.</returns>
        public CommandResult ListPlayersInRoom(PlayerState player, WorldState world)
        {
            // Get a list of all the players in the same room as the player
            var playersInRoom = world.Rooms[player.CurrentLocation].PlayersPresent.ToList();

            // Check if any other players are in the room.
            if (playersInRoom.Count == 0)
            {
                // There are no other players. Return a message indicating that.
                return new CommandResult
                {
                    Success = true,
                    Message = "There are no other players in the same room."
                };
            }
            else
            {
                // There are other players, return a message with their names.
                return new CommandResult
                {
                    Success = true,
                    Message = $"Players in the same room: {string.Join(", ", playersInRoom)}"
                };
            }
        }
    }
}
