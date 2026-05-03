// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Shared.Logging
{
    public sealed record LogRecord(
        LogLevel Level,            // Severity of the log entry
        string Source,             // Origin
        string Message,            // Summary
        object? Data = null,       // Optional structured context
        Guid? ConnectionId = null, // Correlation to a network connection
        Guid? SessionId = null     // Correlation to a session
    );
}
