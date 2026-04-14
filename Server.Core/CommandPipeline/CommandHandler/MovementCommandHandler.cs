using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.Services.Interfaces;

namespace Server.Core.CommandPipeline.CommandHandler
{
    /// <summary>
    /// Handles movement and world-query commands: "move", "go", "look".
    /// Delegates to <see cref="IWorldMovementService"/> and <see cref="IWorldQueryService"/>.
    /// </summary>
    public sealed class MovementCommandHandler : ICommandHandler
    {
        private readonly IWorldMovementService _movementService;
        private readonly IWorldQueryService _queryService;

        /// <summary>
        /// Initializes a new instance of <see cref="MovementCommandHandler"/>.
        /// </summary>
        /// <param name="movementService">The domain service that handles player movement.</param>
        /// <param name="queryService">The domain service that describes rooms.</param>
        public MovementCommandHandler(IWorldMovementService movementService, IWorldQueryService queryService)
        {
            _movementService = movementService;
            _queryService = queryService;
        }

        /// <inheritdoc/>
        public async Task<CommandResult> ExecuteAsync(CommandContext context)
        {
            // Ensure session state has been populated before we do anything.
            if (context.PlayerState is null || context.WorldState is null)
            {
                return new CommandResult { Success = false, Message = "Session state is invalid." };
            }

            string verb = context.Command.CommandType.ToLowerInvariant();

            if (verb == "move" || verb == "go")
            {
                // First argument is the direction the player wants to move (e.g. "north").
                // We take the first argument, if it doesn't exist or is empty, we return an error message.
                string direction = context.Command.Arguments.FirstOrDefault() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(direction))
                {
                    return new CommandResult { Success = false, Message = "Move where? Try: move north" };
                }

                // Perform the movement. This may modify the world state, so we await the result and return it directly.
                return await _movementService.MovePlayerAsync(context.PlayerState, context.WorldState, direction);
            }
            else if (verb == "look")
            {
                // Look is read-only — the snapshot from context is safe to use here.
                return _queryService.LookAtRoom(context.PlayerState, context.WorldState);
            }
            else
            {
                return new CommandResult { Success = false, Message = $"Unknown movement command: {verb}" };
            }
        }
    }
}