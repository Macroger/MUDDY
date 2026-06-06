using Server.Core.CommandPipeline.CommandHandler;
using Server.Core.Infrastructure.Lifecycle;
using Server.Core.Persistence;
using Shared.EventBus;

namespace Server.Core.CommandPipeline.CommandRegistration
{
    public class SystemCommandRegistrar : ICommandRegistrar
    {
        private readonly IServerLifecycle _lifecycle;
        private readonly IPlayerRepository _playerRepository;
        private readonly IEventBus _eventBus;

        public SystemCommandRegistrar(
            IServerLifecycle lifecycle,
            IPlayerRepository playerRepository,
            IEventBus eventBus)
        {
            _lifecycle = lifecycle;
            _playerRepository = playerRepository;
            _eventBus = eventBus;
        }

        public void Register(ICommandRouter router)
        {
            // Create handlers
            var serverStateHandler = new ServerStateCommandHandler(_lifecycle);
            var logoutHandler = new LogoutCommandHandler(_playerRepository, _eventBus);
            var imageHandler = new ImageTransferCommandHandler();

            // Register commands
            router.RegisterHandler("serverstate", serverStateHandler);
            router.RegisterHandler("logout", logoutHandler);
            router.RegisterHandler("sendimage", imageHandler);
        }
    }

}
