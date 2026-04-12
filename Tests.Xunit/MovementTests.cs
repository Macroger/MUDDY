using Xunit;
using System.Collections.Generic;

namespace Muddy.Tests
{
    public class MovementTests
    {
        [Fact]
        public void Move_ValidDirection_ChangesRoom()
        {
            var roomA = new Room("A");
            var roomB = new Room("B");

            roomA.AddExit("north", roomB);

            var player = new Player();
            player.CurrentRoom = roomA;

            bool moved = player.Move("north");

            Assert.True(moved);
            Assert.Equal(roomB, player.CurrentRoom);
        }

        [Fact]
        public void Move_InvalidDirection_ReturnsFalse()
        {
            var room = new Room("A");

            var player = new Player();
            player.CurrentRoom = room;

            bool moved = player.Move("south");

            Assert.False(moved);
        }
    }

    public class Player
    {
        public Room CurrentRoom;

        public bool Move(string direction)
        {
            if (CurrentRoom.Exits.ContainsKey(direction))
            {
                CurrentRoom = CurrentRoom.Exits[direction];
                return true;
            }
            return false;
        }
    }

    public class Room
    {
        public string Name;
        public Dictionary<string, Room> Exits = new Dictionary<string, Room>();

        public Room(string name)
        {
            Name = name;
        }

        public void AddExit(string direction, Room room)
        {
            Exits[direction] = room;
        }
    }
}