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
            roomList.Add(CreateCityRoom());
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
            var roomDescription = "A cozy tavern filled with patrons and the smell of ale.";
            var roomConditions = new HashSet<RoomCondition>();
            var playersPresent = new HashSet<ConnectionId>();
            var roomExits = new Dictionary<string, RoomId>
            {
                { "north", new RoomId("town") } // Exit to the north leading to the forest room.
            };

            return new RoomState(
                id: roomId, 
                description: roomDescription, 
                roomConditions: roomConditions, 
                playersInRoom: playersPresent, 
                exits: roomExits);
        }

        /// <summary>Creates the city room.</summary>
        private static RoomState CreateCityRoom()
        {
            var roomId = new RoomId("town");
            var roomDescription = "A bustling city block with numerous merchants and potential employers.";
            var roomConditions = new HashSet<RoomCondition>();
            var playersPresent = new HashSet<ConnectionId>();
            var roomExits = new Dictionary<string, RoomId>
            {
                { "north", new RoomId("forest") }, // Exit to the north leading to the forest room.
                {  "south", new RoomId("tavern") } // Exit to the south leading back to the tavern room.
            };

            return new RoomState(
                id: roomId,
                description: roomDescription,
                roomConditions: roomConditions,
                playersInRoom: playersPresent,
                exits: roomExits);
        }

        /// <summary>Creates the forest room.</summary>
        private static RoomState CreateForestRoom()
        {
            var roomId = new RoomId("forest");
            var description = "A dense forest with towering trees.";
            var conditions = new HashSet<RoomCondition>();
            var playersPresent = new HashSet<ConnectionId>();
            var roomExits = new Dictionary<string, RoomId>
            {
                { "south", new RoomId("town") } // Exit to the south leading back to the tavern room.
            };

            return new RoomState(
                id: roomId,
                description: description,
                roomConditions: conditions,
                playersInRoom: playersPresent,
                exits: roomExits);
        }
    }
}
