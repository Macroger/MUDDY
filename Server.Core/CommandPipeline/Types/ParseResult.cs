// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Server.Core.CommandPipeline.Types
{
    /// <summary>
    /// Result of parsing a command.
    /// </summary>
    public class ParseResult
    {
        /// <summary>
        /// Indicates whether parsing succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The parsed command (null if parsing failed).
        /// </summary>
        public ParsedCommand? Command { get; set; }

        /// <summary>
        /// Error response to send to client if parsing failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Records the type of error that occurred during parsing, if any.
        /// </summary>
        public CommandErrorType? ErrorType { get; set; }
    }
}