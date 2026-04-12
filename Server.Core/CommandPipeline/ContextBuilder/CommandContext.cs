using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.Player;
using Server.Core.Domain.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.CommandPipeline.ContextBuilder
{
    public class CommandContext
    {
        /// <summary>The original parsed command.</summary>
        public ParsedCommand Command { get; init; }

        /// <summary>Current state of the player executing the command.</summary>
        public PlayerState? PlayerState { get; init; }

        /// <summary>Current state of the game world.</summary>
        public WorldState? WorldState { get; init; }

        /// <summary>Indicates if context building succeeded.</summary>
        public bool Success { get; init; }

        /// <summary>Error details if context building failed.</summary>
        public string? ErrorMessage { get; init; }

        public CommandContext(
            ParsedCommand command,
            PlayerState? playerState,
            WorldState? worldState,
            bool success,
            string? errorMessage)
        {
            Command = command;
            PlayerState = playerState;
            WorldState = worldState;
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
}
