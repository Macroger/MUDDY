using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.Services.Interfaces;

namespace Server.Core.CommandPipeline.CommandHandler
{
    /// <summary>
    /// Handles player-info commands: "status", "who".
    /// Delegates to <see cref="IPlayerQueryService"/>.
    /// </summary>
    public sealed class PlayerCommandHandler : ICommandHandler
    {
        private readonly IPlayerQueryService _playerQueryService;

        /// <summary>
        /// Initializes a new instance of <see cref="PlayerCommandHandler"/>.
        /// </summary>
        /// <param name="playerQueryService">The domain service that queries player information.</param>
        public PlayerCommandHandler(IPlayerQueryService playerQueryService)
        {
            _playerQueryService = playerQueryService;
        }

        /// <inheritdoc/>
        public Task<CommandResult> ExecuteAsync(CommandContext context)
        {
            // Ensure session state has been populated before we do anything.
            if (context.PlayerState is null || context.WorldState is null)
            {
                return Task.FromResult(new CommandResult { Success = false, Message = "Session state is invalid." });
            }

            // Convert the command verb to lowercase for case-insensitive matching.
            string verb = context.Command.CommandType.ToLowerInvariant();
            CommandResult result;

            if (verb == "status")
            {
                // Returns the player's name, current location, and any active conditions.
                result = _playerQueryService.GetPlayerStatus(context.PlayerState, context.WorldState);
            }
            else if (verb == "who")
            {
                // Returns a list of other players currently in the same room.
                result = _playerQueryService.ListPlayersInRoom(context.PlayerState, context.WorldState);
            }
            else
            {
                result = new CommandResult { Success = false, Message = $"Unknown player command: {verb}" };
            }

            // Task.FromResult wraps the finished value - to keep the proper signature of the method, which is async.
            return Task.FromResult(result);
        }
    }
}