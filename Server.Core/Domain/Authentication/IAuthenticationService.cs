// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0

using Shared.Identity;

namespace Server.Core.Domain.Authentication
{
    public interface IAuthenticationService
    {
        Task<bool> ValidateSessionAsync(SessionId? sessionId, ConnectionId connId);

        Task<SessionId> CreateSessionAsync(ConnectionId connId, string playerName);

        Task RemoveSession(SessionId sessionId);

        Task RemoveSessionByConnectionIdAsync(ConnectionId connId);
    }
}
