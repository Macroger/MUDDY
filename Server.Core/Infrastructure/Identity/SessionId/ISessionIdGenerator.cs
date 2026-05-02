// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Server.Core.Infrastructure.Identity.SessionId
{
    public interface ISessionIdGenerator
    {
        Shared.Identity.SessionId New();
    }

}
