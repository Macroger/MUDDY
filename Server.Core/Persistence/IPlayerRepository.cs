// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.Domain.Player;
using Shared.Identity;

namespace Server.Core.Persistence
{
    public interface IPlayerRepository
    {
        public Task<PlayerState?> GetPlayerByConnectionIdAsync(ConnectionId connId);
        public Task<PlayerState?> GetPlayerByNameAsync(string playerName);

        public Task UpsertPlayerAsync(PlayerState player);

        public Task<bool> RemovePlayerAsync(ConnectionId connId);
    }
}
