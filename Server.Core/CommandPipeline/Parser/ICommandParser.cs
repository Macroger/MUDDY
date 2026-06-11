// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.Types;
using Shared.Network.Transport;

namespace Server.Core.CommandPipeline.Parser
{
    public interface ICommandParser
    {
        /// <summary>
        /// Parses a transport envelope containing a JSON command into a structured command.
        /// </summary>
        /// <param name="envelope">The transport envelope with JSON command body.</param>
        /// <returns>Parse result with parsed command or error information.</returns>
        ParseResult Parse(PacketEnvelope msg);
    }
}