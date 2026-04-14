namespace Server.Core.CommandPipeline.ContextBuilder
{
    public enum RoomCondition
    {
        MANA_BLESSED,           // The room is currently blessed with mana, which may enhance magical effects and certain player actions.
        MANA_CURSED,            // The room is currently cursed with mana, which may weaken magical effects and certain player actions.
        EXPERIENCE_BOOSTED,     // The room is currently blessed with an experience boost, which may increase the amount of experience points gained from actions performed in the room.
        EXPERIENCE_DRAINED      // The room is currently cursed with an experience drain, which may decrease the amount of experience points gained from actions performed in the room.
    }
}
