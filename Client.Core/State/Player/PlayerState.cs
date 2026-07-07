using Shared.Domain.Player;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Core.State.Player
{
    public class PlayerState
    {
        public required string PlayerName { get; set; }
        public required int Level { get; set; }
        public required int MaxHp { get; set; }
        public required int CurrentHp { get; set; }

        public required int MaxMp { get; set; }
        public required int CurrentMp { get; set; }
        public required int Experience { get; set; }
        public required int Currency { get; set; }

        public required IReadOnlySet<PlayerCondition> ActiveConditions { get; set; }

     
        public required PlayerLocation Location { get; set; }

    }
}
/*
        * Things we need to track for playerstate:
        * First Tier:
        * Player Name:
        * Player Max HP:
        * Player Current HP:
        * Player Max MP:
        * Player Current MP:
        * Player Location:
        * Player Class:
        * Player Level:
        * Player Experience:
        * Player Inventory:
        * Player Equipment:
        * Player Skills:
        * Player Quests:
        * Player Currency:
        * PlayerConditions:
        */