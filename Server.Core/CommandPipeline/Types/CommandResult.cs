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
    }
}
