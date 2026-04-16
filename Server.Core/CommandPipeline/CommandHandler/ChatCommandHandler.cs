using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.Services.ChatService;

namespace Server.Core.CommandPipeline.CommandHandler
{
    /// <summary>
    /// Handles chat-related commands: "say".
    /// Delegates all logic to <see cref="IChatService"/>.
    /// </summary>
    public sealed class ChatCommandHandler : ICommandHandler
    {
        private readonly IChatService _chatService;

        /// <summary>
        /// Initializes a new instance of <see cref="ChatCommandHandler"/>.
        /// </summary>
        /// <param name="chatService">The domain service that handles chat messages.</param>
        public ChatCommandHandler(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <inheritdoc/>
        public async Task<CommandResult> ExecuteAsync(CommandContext context)
        {
            // Ensure session state has been populated before we do anything.
            if (context.PlayerState is null || context.WorldState is null)
            {
                return new CommandResult { Success = false, Message = "Session state is invalid." };
            }

            // Combine all arguments back into a single message string.
            string message = string.Join(" ", context.Command.Arguments);
            string verb = context.Command.CommandType.ToLowerInvariant();

            if (verb == "say")
            {
                // Make sure the player actually typed something after "say".
                if (string.IsNullOrWhiteSpace(message))
                {
                    return new CommandResult { Success = false, Message = "Say what?" };
                }

                // Delegate to ChatService — it handles broadcasting to the room.
                return await _chatService.BroadcastMessageAsync(context.PlayerState, context.WorldState, message);
            }
            else
            {
                return new CommandResult { Success = false, Message = $"Unknown chat command: {verb}" };
            }
        }
    }
}
