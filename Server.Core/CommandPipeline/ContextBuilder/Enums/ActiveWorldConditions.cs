namespace Server.Core.CommandPipeline.ContextBuilder
{
    public enum ActiveWorldConditions
    {
        RAINING,            // The world is currently experiencing rain, which may affect visibility and certain player actions.
        SNOWING,            // The world is currently experiencing snow, which may affect movement and certain player actions.
        FOGGY,              // The world is currently experiencing fog, which may reduce visibility and affect certain player actions.
        MANA_VORTEX,        // The world is currently affected by a mana vortex, which may cause magical effects to behave unpredictably and affect certain player actions.
        EARTHQUAKE,         // The world is currently experiencing an earthquake, which may cause tremors and affect certain player actions.
        SOLAR_ECLIPSE       // The world is currently experiencing a solar eclipse, which may affect visibility and certain player actions (spells).
    }
}
