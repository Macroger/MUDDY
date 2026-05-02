// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Server.Core.Infrastructure.Identity.ConnectionId
{
    public interface IConnectionIdGenerator
    {
        Shared.Identity.ConnectionId New();
    }
}
