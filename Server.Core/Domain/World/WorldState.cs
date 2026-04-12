using Server.Core.CommandPipeline.ContextBuilder;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Domain.World
{
    public class WorldState
    {
        public IReadOnlyDictionary<string, RoomState> Rooms { get; init; }
        public IReadOnlySet<ActiveWorldConditions> GlobalConditions { get; init; } = new HashSet<ActiveWorldConditions>();

        public WorldState(IReadOnlyDictionary<string, RoomState> rooms, IReadOnlySet<ActiveWorldConditions> globalConditions)
        {
            Rooms = rooms;
            GlobalConditions = globalConditions;
        }
    }
}
