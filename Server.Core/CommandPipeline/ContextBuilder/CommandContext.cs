// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.World;
using Shared.Domain.Player;

namespace Server.Core.CommandPipeline.ContextBuilder
{
    public record CommandContext
    {
        /// <summary>The original parsed command.</summary>
        public ParsedCommand Command { get; init; }

        /// <summary>Indicates if context building succeeded.</summary>
        public bool Success { get; init; }

        /// <summary>Error details if context building failed.</summary>
        public string? ErrorMessage { get; init; }

        public CommandContext(
            ParsedCommand command,
            bool success,
            string? errorMessage)
        {
            Command = command;
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
}
