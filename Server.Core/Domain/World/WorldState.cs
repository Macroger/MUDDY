// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.ContextBuilder;
using Shared.Identity;

namespace Server.Core.Domain.World
{
    public class WorldState
    {
        public IReadOnlyDictionary<RoomId, RoomState> Rooms { get; init; }
        public IReadOnlySet<ActiveWorldConditions> GlobalConditions { get; init; } = new HashSet<ActiveWorldConditions>();

        public WorldState(IReadOnlyDictionary<RoomId, RoomState> rooms, IReadOnlySet<ActiveWorldConditions> globalConditions)
        {
            Rooms = rooms;
            GlobalConditions = globalConditions;
        }
    }
}
