// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Shared.Protocol.System
{

    /// <summary>
    /// Represents a structured system response code with severity, human-readable message and retry semantics.
    /// </summary>
    public sealed class SystemResponse
    {
        /// <summary>The machine-readable response code describing the type of system response.</summary>
        public SystemResponseType Code { get; }

        /// <summary>The severity level for the response.</summary>
        public SystemResponseSeverity Severity { get; }

        /// <summary>Human readable message describing the response.</summary>
        public string Message { get; }

        /// <summary>Indicates whether the client may retry the operation that produced this response.</summary>
        public bool Retryable { get; }

        /// <summary>Constructs a new SystemResponse instance.</summary>
        /// <param name="code">The response code.</param>
        /// <param name="severity">The severity of the response.</param>
        /// <param name="message">Human readable message describing the response.</param>
        /// <param name="retryable">Whether the client may retry the operation.</param>
        public SystemResponse(
            SystemResponseType code,
            SystemResponseSeverity severity,
            string message,
            bool retryable)
        {
            Code = code;
            Severity = severity;
            Message = message;
            Retryable = retryable;
        }
    }

}
