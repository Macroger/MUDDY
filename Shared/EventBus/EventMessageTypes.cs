
namespace Shared.EventBus
{

    public enum EventMessageType
    {
        Log,            // Structured logging events
        System,         // Server/process lifecycle & global state
        Network,        // Server-side networking and connections
        ClientNetwork,  // Client-side networking 
        Protocol,       // Protocol-level processing (parsing, validation)
        Command,        // Command parsing and execution pipeline
        Domain,         // Game-world/domain events (rooms, items, players)
        Persistence,    // Database, saving/loading, storage operations
        Error           // Cross-cutting error reporting
    }

}
