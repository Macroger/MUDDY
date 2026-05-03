// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;

namespace Server.Core.CommandPipeline.CommandHandler
{
    /// <summary>
    /// Defines the contract for command handlers that execute parsed commands.
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary>
        /// Executes the command with the given context and returns the result.
        /// </summary>
        /// <param name="context">The enriched command context with player and world state.</param>
        /// <returns>A task representing the command execution result.</returns>
        Task<CommandResult> ExecuteAsync(CommandContext context);
    }
}
