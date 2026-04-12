using Server.Core.Domain.World;
using Shared.Identity;

namespace Server.Core.Persistence
{
    public interface IWorldRepository
    {
        /// <summary>Gets the current world state.</summary>
        Task<WorldState> GetWorldStateAsync();

        /// <summary>Gets a specific room's state.</summary>
        Task<RoomState?> GetRoomAsync(RoomId roomId);
    }
}
