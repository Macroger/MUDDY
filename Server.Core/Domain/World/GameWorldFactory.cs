using Server.Core.CommandPipeline.ContextBuilder;
using Shared.Identity;

namespace Server.Core.Domain.World
{
    /// <summary>
    /// Factory for creating the default game world and all its rooms.
    /// This is the single place to define the world layout for v1.
    /// </summary>
    public static class GameWorldFactory
    {
        /// <summary>
        /// The room ID where new players spawn when they log in.
        /// </summary>
        public static readonly RoomId StartingRoomId = new RoomId("tavern");
       

        /// <summary>
        /// Creates the default world state with all rooms initialized.
        /// </summary>
        public static WorldState CreateDefaultWorld()
        {
            // Create a list and add any rooms we want in the world. This is where we define the world layout.
            List<RoomState> roomList = new();

            // Add new rooms by creating a method and adding it to the list here.
            roomList.Add(CreateTavernRoom());
            roomList.Add(CreateForestRoom());

            // Crete a dictionary to hold the rooms, using the room ID as the key for easy lookup.
            Dictionary<RoomId, RoomState> rooms = new();

            // Populate the dictionary with the rooms from the list.
            foreach (RoomState room in roomList)
            {
                rooms.Add(room.Id, room);
            }

            // Create a world state from the rooms and an empty set of global conditions (we can add global conditions later as needed).
            WorldState worldState = new WorldState(
                rooms,
                new HashSet<ActiveWorldConditions>()
            );

            return worldState;
        }

        /// <summary>Creates the tavern room.</summary>
        private static RoomState CreateTavernRoom()
        {
            var roomId = new RoomId("tavern");
            var description = "A cozy tavern filled with patrons and the smell of ale.";
            var conditions = new HashSet<RoomCondition>();
            var playersPresent = new HashSet<ConnectionId>();

            return new RoomState(roomId, description, conditions, playersPresent);
        }

        /// <summary>Creates the forest room.</summary>
        private static RoomState CreateForestRoom()
        {
            var roomId = new RoomId("forest");
            var description = "A dense forest with towering trees.";
            var conditions = new HashSet<RoomCondition>();
            var playersPresent = new HashSet<ConnectionId>();

            return new RoomState(roomId, description, conditions, playersPresent);
        }
    }
}
