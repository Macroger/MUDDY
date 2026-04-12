using Server.Core.Domain.Player;
using Shared.Identity;
using System;
using System.Collections.Generic;
using System.Text;

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
