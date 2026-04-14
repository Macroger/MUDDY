using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.Domain.World;
using Shared.Identity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Persistence
{
    public class InMemoryWorldRepository : IWorldRepository
    {

        // Each room is stored by its RoomId. ConcurrentDictionary keeps this thread-safe.
        private readonly ConcurrentDictionary<RoomId, RoomState> _rooms = new();

        // Global conditions that apply to the whole world (e.g. "night", "raining").
        private IReadOnlySet<ActiveWorldConditions> _globalConditions = new HashSet<ActiveWorldConditions>();

        private WorldState _worldState;
        public InMemoryWorldRepository()
        {
            _worldState = GameWorldFactory.CreateDefaultWorld();
        }

        /// <summary>
        /// Seeds the repository with the initial world layout from the factory.
        /// Called once at startup before any players connect.
        /// </summary>
        public void Seed(WorldState initialWorld)
        {
            foreach (var room in initialWorld.Rooms.Values)
            {
                _rooms[room.Id] = room;
            }

            _globalConditions = initialWorld.GlobalConditions;
        }


        public async Task<RoomState?> GetRoomAsync(RoomId roomId)
        {
            // Try to get the room state from the world state using the provided room ID.
            _worldState.Rooms.TryGetValue(roomId, out var room);

            // If the room was found, return it.
            return await Task.FromResult(room);
        }

        public Task<WorldState> GetWorldStateAsync()
        {
            // Build a fresh WorldState snapshot from the current room dictionary.
            var snapshot = new WorldState(
                rooms: new Dictionary<RoomId, RoomState>(_rooms),
                globalConditions: _globalConditions
            );

            return Task.FromResult(snapshot);
        }

        /// <inheritdoc/>
        public Task UpdateRoomAsync(RoomState room)
        {
            // AddOrUpdate replaces whatever was stored for this RoomId with the new version.
            _rooms.AddOrUpdate(
                key: room.Id, 
                addValue: room, 
                updateValueFactory: (_, _) => room
            );

            return Task.CompletedTask;
        }
    }
}
