using Shared.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Domain.Player
{
    public interface IPlayerRepository
    {
        /// <summary>Gets a player's state by connection ID.</summary>
        Task<PlayerState?> GetPlayerByConnectionIdAsync(ConnectionId connId);

        /// <summary>Gets a player's state by player name.</summary>
        Task<PlayerState?> GetPlayerByNameAsync(string playerName);

        /// <summary>Adds or updates a player in the repository.</summary>
        Task UpsertPlayerAsync(PlayerState player);

        /// <summary>Removes a player from the repository.</summary>
        Task RemovePlayerAsync(ConnectionId connId);
    }
}
