
namespace Shared.EventBus
{

    public enum EventMessageType
    {
        Authentication, // Login/logout, session management
        Log,            // Structured logging events
        System,         // Server/process lifecycle & global state
        Network,        // Server-side networking and connections
        ClientNetwork,  // Client-side networking 
        Protocol,       // Protocol-level processing (parsing, validation)
        Command,        // Command parsing and execution pipeline
        Domain,         // Game-world/domain events (rooms, items, players)
        Gui,            // GUI events, such as updates to the client UI or notifications
        Persistence,    // Database, saving/loading, storage operations
        Chat,           // In-game chat messages and events
        Error           // Cross-cutting error reporting
    }

}
