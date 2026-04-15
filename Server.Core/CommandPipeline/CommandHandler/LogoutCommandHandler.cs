using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;
using Server.Core.Persistence;
using Shared.EventBus;

namespace Server.Core.CommandPipeline.CommandHandler
{
    /// <summary>
    /// Handles the 'logout' command for authenticated players.
    /// </summary>
    public sealed class LogoutCommandHandler : ICommandHandler
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly IEventBus _eventBus;

        public LogoutCommandHandler(IPlayerRepository playerRepository, IEventBus eventBus)
        {
            _playerRepository = playerRepository;
            _eventBus = eventBus;
        }

        public async Task<CommandResult> ExecuteAsync(CommandContext context)
        {
            if (context.PlayerState is null)
            {
                return new CommandResult { Success = false, Message = "No player session found." };
            }

            // Remove player from repository (triggers PlayerLeftWorldEvent via repo logic)
            await _playerRepository.RemovePlayerAsync(context.PlayerState.ConnId);

            // Optionally, send a confirmation message
            return new CommandResult { Success = true, Message = "You have been logged out. Goodbye!" };
        }
    }
}
