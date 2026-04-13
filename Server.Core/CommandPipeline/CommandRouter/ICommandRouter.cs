using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;

namespace Server.Core.CommandPipeline
{
    public interface ICommandRouter
    {
        /// <summary>
        /// Routes a parsed command to its corresponding handler.
        /// </summary>
        /// <param name="command">The parsed command with verb and arguments.</param>
        /// <returns>The matching command handler, or null if the command is unknown.</returns>
        ICommandHandler? Route(CommandContext command);
    }
}