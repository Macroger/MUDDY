using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.Player;
using Server.Core.Domain.Services.Interfaces;
using Server.Core.Domain.World;
using Server.Core.Persistence;
using Shared.Identity;

namespace Server.Core.Domain.Services.ConcreteClasses
{
    /// <summary>
    /// Handles player movement between rooms.
    /// </summary>
    public class WorldMovementService : IWorldMovementService
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly IWorldRepository _worldRepository;

        /// <summary>
        /// Initializes a new instance of <see cref="WorldMovementService"/>.
        /// </summary>
        public WorldMovementService(IPlayerRepository playerRepository, IWorldRepository worldRepository)
        {
            _playerRepository = playerRepository;
            _worldRepository = worldRepository;
        }

        /// <inheritdoc/>
        public async Task<CommandResult> MovePlayerAsync(PlayerState snapshotPlayer, WorldState snapshotWorld, string direction)
        {
            // Re-fetch the live player — don't trust the context snapshot for a write operation.
            PlayerState? livePlayer = await _playerRepository.GetPlayerByConnectionIdAsync(snapshotPlayer.ConnId);
            if (livePlayer is null)
            {
                return new CommandResult { Success = false, Message = "Could not find your player data. Please reconnect." };
            }

            // Get the player's current room using their live location.
            RoomState? currentRoom = await _worldRepository.GetRoomAsync(livePlayer.CurrentLocation);
            if (currentRoom is null)
            {
                return new CommandResult { Success = false, Message = "Your current room could not be found." };
            }

            // Check if an exit exists in the requested direction.
            // Exits use OrdinalIgnoreCase so "North" and "north" both work.
            if (!currentRoom.Exits.TryGetValue(direction, out RoomId destinationRoomId))
            {
                return new CommandResult { Success = false, Message = $"There is no exit to the {direction}." };
            }

            // Get the destination room.
            RoomState? destinationRoom = await _worldRepository.GetRoomAsync(destinationRoomId);
            if (destinationRoom is null)
            {
                return new CommandResult { Success = false, Message = $"The exit to the {direction} leads nowhere. (Missing room data)" };
            }

            // Because RoomState is immutable, we create a new instance with an updated set.
            HashSet<ConnectionId> newPreviousRoomPlayers = new HashSet<ConnectionId>(currentRoom.PlayersPresent);

            // Remove the player from the previous room's presence list.
            newPreviousRoomPlayers.Remove(livePlayer.ConnId);

            // Generate the new RoomState for the player's old room with the updated presence list.
            RoomState updatedOldRoom = new RoomState(
                id: currentRoom.Id,
                description: currentRoom.Description,
                roomConditions: currentRoom.Conditions,
                playersInRoom: newPreviousRoomPlayers,
                exits: currentRoom.Exits
            );

            // Add the player to their new room's presence list.
            HashSet<ConnectionId> updatedNewRoomPlayers = new HashSet<ConnectionId>(destinationRoom.PlayersPresent);
            updatedNewRoomPlayers.Add(livePlayer.ConnId);

            RoomState updatedNewRoom = new RoomState(
                id: destinationRoom.Id,
                description: destinationRoom.Description,
                roomConditions: destinationRoom.Conditions,
                playersInRoom: updatedNewRoomPlayers,
                exits: destinationRoom.Exits
            );

            // Update the player's stored location.
            PlayerState movedPlayer = new PlayerState
            {
                ConnId = livePlayer.ConnId,
                PlayerName = livePlayer.PlayerName,
                CurrentLocation = destinationRoomId,
                ActiveConditions = livePlayer.ActiveConditions
            };

            // Persist all three changes.
            await _worldRepository.UpdateRoomAsync(updatedOldRoom);
            await _worldRepository.UpdateRoomAsync(updatedNewRoom);
            await _playerRepository.UpsertPlayerAsync(movedPlayer);

            return new CommandResult
            {
                Success = true,
                Message = $"You move {direction}.\n\n{destinationRoom.Description}"
            };
        }
    }
}