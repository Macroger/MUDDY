// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Shared.Logging
{
    public enum LogLevel
    {
        Trace,      // Extremely detailed flow details (usually disabled)
        Debug,      // Developer diagnostics and state inspection
        Information,// Normal, expected system behavior
        Warning,    // Unexpected but recoverable situations
        Error,      // Failures that break the current operation
        Fatal       // Unrecoverable failures requiring shutdown
    }
}
