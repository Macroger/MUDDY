// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.CommandHandler;
using Server.Core.CommandPipeline.Types;

namespace Server.Core.CommandPipeline
{
    public interface ICommandRouter
    {

        /// <summary>
        /// Registers a handler for a specific command verb.
        /// </summary>
        /// <param name="verb">The command verb.</param>
        /// <param name="handler">The handler to execute for this verb.</param>
        /// <returns>True if the handler was registered; false if a handler already exists for this verb.</returns>
        bool RegisterHandler(string verb, ICommandHandler handler);

        /// <summary>
        /// Returns a parsed command's corresponding handler.
        /// </summary>
        /// <param name="command">The parsed command with verb and arguments.</param>
        /// <returns>The matching command handler, or null if the command is unknown.</returns>
        ICommandHandler? GetHandler(ParsedCommand command);
    }
}