namespace Shared.EventBus.EventTypes
{
    public enum EventMessageType
    {
        Authentication, // Login/logout, session management
        Chat,           // In-game chat messages and events        
        CmdPipeline,    // Command parsing and execution pipeline
        GameTick,       // Regular tick/update events for game logic processing
        Gui,            // GUI events, such as updates to the client UI or notifications
        Metrics,        // Performance metrics, analytics, and monitoring events
        Network,        // Server-side networking and connections
        Persistence,    // Database, saving/loading, storage operations
        Player,         // Player-specific events, such as health changes, inventory updates, etc.       
        System,         // Server/process lifecycle & global state
        World,          // World state changes, such as entity creation/destruction, movement, etc.
    }
}
