// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.Types;
using Shared.Identity;

namespace Server.Core.CommandPipeline.ContextBuilder
{
    public interface ICommandContextBuilder
    {
        /// <summary>
        /// Builds an enriched command context containing player and world state information.
        /// Called after parsing but before post-parse policy checks.
        /// </summary>
        Task<CommandContext> BuildContextAsync(ConnectionId connId, ParsedCommand command);
    }
}
