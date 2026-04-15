using Server.Core.CommandPipeline.Types;
using Shared.Domain.Player;
using Server.Core.Domain.World;

namespace Server.Core.Domain.Services.Interfaces
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
