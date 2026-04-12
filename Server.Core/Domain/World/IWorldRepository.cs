using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Domain.World
{
    public interface IWorldRepository
    {
        /// <summary>Gets the current world state.</summary>
        Task<WorldState> GetWorldStateAsync();

        /// <summary>Gets a specific room's state.</summary>
        Task<RoomState?> GetRoomAsync(string roomId);
    }
}
