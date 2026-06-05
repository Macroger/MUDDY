// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.Domain.World;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;
using Shared.Identity;
using System.Collections.Concurrent;

namespace Server.Core.Persistence
{
    public class InMemoryWorldRepository : IWorldRepository
    {

        private readonly IEventBus _eventBus;
        private readonly ISubscriptionToken _playerLeftSubscription;

        // Each room is stored by its RoomId. ConcurrentDictionary keeps this thread-safe.
        // This is the single authoritative source for all room state — snapshots are built from this.
        private readonly ConcurrentDictionary<RoomId, RoomState> _rooms = new();

        // Global conditions that apply to the whole world (e.g. "night", "raining").
        private IReadOnlySet<ActiveWorldConditions> _globalConditions = new HashSet<ActiveWorldConditions>();

        public InMemoryWorldRepository(IEventBus eventBus)
        {
            _eventBus = eventBus;
            // Pre-populate the authoritative rooms dictionary with the default world layout.
            Seed(GameWorldFactory.CreateDefaultWorld());
            _playerLeftSubscription = _eventBus.Subscribe<PlayerEvents.PlayerLeftWorldEvent>(
                EventMessageType.Domain, HandlePlayerLeft);
        }

        private async void HandlePlayerLeft(PlayerEvents.PlayerLeftWorldEvent evt)
        {
            var room = await GetRoomAsync(evt.LastRoom);
            if (room is null) return;

            var updated = new HashSet<ConnectionId>(room.PlayersPresent);
            updated.Remove(evt.ConnId);
            await UpdateRoomAsync(new RoomState(room.Id, room.Description, room.Conditions, updated, room.Exits));
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
            // Read from the authoritative rooms dictionary, not a frozen snapshot.
            _rooms.TryGetValue(roomId, out var room);
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
