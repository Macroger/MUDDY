// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.Identity;

namespace Server.Core.CommandPipeline.Types
{
    /// <summary>
    /// A structured command extracted from a transport envelope.
    /// </summary>
    public class ParsedCommand
    {
        /// <summary>
        /// The command verb (e.g., "move", "attack", "say").
        /// </summary>
        public string CommandType { get; set; } = string.Empty;

        /// <summary>
        /// The raw arguments passed to the command (e.g., "north" for "move north").
        /// </summary>
        public string[] Arguments { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The original message ID for correlation/logging.
        /// </summary>
        public MessageId MsgId { get; set; }
    }
}