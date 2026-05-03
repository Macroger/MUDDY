// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Server.Core.Infrastructure.Lifecycle
{
    public enum ServerStateEnum
    {
        LOADING,
        ACTIVE,
        MAINTENANCE,
        SHUTTING_DOWN
    }
}
