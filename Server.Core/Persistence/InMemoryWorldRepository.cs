using Server.Core.Domain.World;
using Shared.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Persistence
{
    public class InMemoryWorldRepository : IWorldRepository
    {
        private WorldState _worldState;
        public InMemoryWorldRepository()
        {
            _worldState = GameWorldFactory.CreateDefaultWorld();
        }


        public async Task<RoomState?> GetRoomAsync(RoomId roomId)
        {
            // Try to get the room state from the world state using the provided room ID.
            _worldState.Rooms.TryGetValue(roomId, out var room);

            // If the room was found, return it.
            return await Task.FromResult(room);
        }

        public async Task<WorldState> GetWorldStateAsync()
        {
            return await Task.FromResult<WorldState>(_worldState);
        }
    }
}
