using Shared.Domain.Player;

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