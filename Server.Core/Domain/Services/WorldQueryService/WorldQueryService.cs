using Server.Core.CommandPipeline.Types;
using Shared.Domain.Player;
using Server.Core.Domain.World;

namespace Server.Core.Domain.Services.WorldQueryService
{
    public class WorldQueryService : IWorldQueryService
    {
        public CommandResult LookAtRoom(PlayerState player, WorldState world)
        {
            // Get the roomState for the player's current location
            RoomState playerRoom = world.Rooms[player.CurrentLocation];

            if (playerRoom == null)
            {
                // If the roomState is null, return a commandResult indicating failure
                CommandResult errorResult = new CommandResult
                {
                    Success = false,
                    Message = "Unable to determine room location. You are lost in a sea of stars."
                };
                return errorResult;
            }

            // Get the rooms description and return a commandResult containing it
            CommandResult result = new CommandResult
            {
                Success = true,
                Message = playerRoom.Description ?? "You see nothing of interest here."
            };
            return result;
        }
    }
}
