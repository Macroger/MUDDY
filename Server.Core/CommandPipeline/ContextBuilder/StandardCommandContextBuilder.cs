using Server.Core.CommandPipeline.Types;
using Shared.Domain.Player;
using Server.Core.Domain.World;
using Server.Core.Persistence;
using Shared.Identity;

namespace Server.Core.CommandPipeline.ContextBuilder
{
    public class StandardCommandContextBuilder : ICommandContextBuilder
    {

        private readonly IPlayerRepository _playerRepository;
        private readonly IWorldRepository _worldRepository;

        public StandardCommandContextBuilder(
            IPlayerRepository playerRepository, 
            IWorldRepository worldRepository)
        {
            _playerRepository = playerRepository;
            _worldRepository = worldRepository;
        }
        public async Task<CommandContext> BuildContextAsync(ConnectionId connId, ParsedCommand command)
        {
            // Get the player via their connId.
            PlayerState? player = await _playerRepository.GetPlayerByConnectionIdAsync(connId);

            // Check if a player was found for the given connection ID.
            if (player == null)
            {
                // No player was found - generate an error response context.
                CommandContext errorResponse = new CommandContext(
                    command: command,
                    playerState: null,
                    worldState: null,
                    success: false,
                    errorMessage: $"No player found for connection ID: {connId}"
                );

                return await Task.FromResult(errorResponse);
            }

            RoomState? currentRoomState = await _worldRepository.GetRoomAsync(player.CurrentLocation);

            if(currentRoomState == null)
            {
                CommandContext errorResponse = new CommandContext(
                    command: command,
                    playerState: player,
                    worldState: null,
                    success: false,
                    errorMessage: $"Player's room '{player.CurrentLocation.Value}' not found in world."
                );

                return await Task.FromResult(errorResponse);
            }

            WorldState currentWorldState = await _worldRepository.GetWorldStateAsync();
            CommandContext successResponse = new CommandContext(
                command: command,
                playerState: player,
                worldState: currentWorldState,
                success: true,
                errorMessage: null
            );
            return await Task.FromResult(successResponse);

        }
    }
}
