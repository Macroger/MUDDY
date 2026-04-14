using Shared.Identity;

namespace Server.Core.CommandPipeline.Types
{
    public class CommandResult
    {
        /// <summary>
        /// Whether the command executed successfully.
        /// </summary>
        public required bool Success { get; init; }

        /// <summary>
        /// The response message to send to the player.
        /// </summary>
        public required string Message { get; init; }

        /// <summary>
        /// Optional additional connections that should also receive this message.
        /// Used for broadcast commands such as chat, emotes, or room notifications.
        /// </summary>
        public IReadOnlySet<ConnectionId>? AdditionalRecipients { get; init; }
    }
}
