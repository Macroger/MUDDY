using Server.Core.CommandPipeline.CommandHandler;
using Server.Core.Domain.Services.PlayerQueryService;

namespace Server.Core.CommandPipeline.CommandRegistration
{

    public class PlayerCommandRegistrar : ICommandRegistrar
    {
        private readonly IPlayerQueryService _playerQueryService;

        private static readonly string[] Commands =
        {
            "status", "who", "player"
        };

        public PlayerCommandRegistrar(IPlayerQueryService playerQueryService)
        {
            _playerQueryService = playerQueryService;
        }

        public void Register(ICommandRouter router)
        {
            var handler = new PlayerCommandHandler(_playerQueryService);
            foreach (var cmd in Commands)
            {
                router.RegisterHandler(cmd, handler);
            }
        }
    }
}
