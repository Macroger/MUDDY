using Server.Core.CommandPipeline.CommandHandler;
using Server.Core.Domain.Services.WorldMovementService;
using Server.Core.Domain.Services.WorldQueryService;

namespace Server.Core.CommandPipeline.CommandRegistration
{
    public class MovementCommandRegistrar : ICommandRegistrar
    {
        private static readonly string[] Commands =
        {
            "move", "go", "look",
            "north", "south", "east", "west",
            "northeast", "northwest",
            "southeast", "southwest",
            "up", "down"
        };       

        private readonly IWorldMovementService _movement;
        private readonly IWorldQueryService _query;

        public MovementCommandRegistrar(IWorldMovementService movement, IWorldQueryService query)
        {
            _movement = movement;
            _query = query;
        }

        public void Register(ICommandRouter router)
        {
            var handler = new MovementCommandHandler(_movement, _query);

            foreach (var cmd in Commands)
            {
                router.RegisterHandler(cmd, handler);
            }
        }
    }
}
