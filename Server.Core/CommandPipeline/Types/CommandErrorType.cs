// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Server.Core.CommandPipeline.Types
{
    public enum CommandErrorType
    {
        INVALID_JSON,
        MISSING_VERB,
        UNKNOWN_ERROR
    }
}