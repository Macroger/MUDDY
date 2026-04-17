using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.World;
using Shared.Domain.Player;

namespace Server.Core.Domain.Services.ChatService
{
    /// <summary>
    /// Handles chat-related operations in the game world.
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Broadcasts a message from a player to all players in their current room.
        /// </summary>
        Task<CommandResult> BroadcastMessageAsync(
            PlayerState sender,
            WorldState world,
            string message);
    }
}
