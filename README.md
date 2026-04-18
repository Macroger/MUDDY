# MUDDY - Multi-User Domain for Dynamic Learning

**Version:** 1.0  
**Framework:** .NET 10  
**Architecture:** Event-driven pipeline with async TCP networking

## Project Overview

MUDDY is a text-based multiplayer game server and client application built with modern .NET technologies. The system demonstrates advanced software engineering concepts including event-driven architecture, async I/O, command pipeline processing, and custom binary network protocols.

**Repository:** COMP72070-26W-Section1-Group3  
**Purpose:** Semester project demonstrating client-server architecture and OOP design

## Architecture Summary

MUDDY uses a **pipeline-based command processor** with a **pub/sub event system**. Clients connect via TCP, send JSON commands, and the server processes them through a multi-stage pipeline (authentication, parsing, validation, execution) before responding with game state updates.

### Core Components

**Server Stack:**
- Server.Core - Command pipeline, networking, domain logic, persistence
- Server.GUI - WinUI 3 admin dashboard for server management

**Client Stack:**
- Client.Core - Networking layer and message routing
- Client.GUI - WinUI 3 game client interface

**Shared Libraries:**
- Protocol layer (TransportEnvelope, MuddyPacket serialization)
- Event bus (BasicEventBus with 15+ channels)
- Domain models (PlayerState, RoomState, WorldState)
- Identity types (ConnectionId, SessionId, MessageId, RoomId)
- Logging infrastructure (PacketLogger, FileLogger, EventBusLogger)

### Key Technologies

- **.NET 10** - Latest C# features and performance
- **WinUI 3** - Modern Windows UI framework
- **TCP/IP** - Custom binary protocol with JSON payloads
- **Async/Await** - Non-blocking I/O throughout
- **ConcurrentDictionary** - Thread-safe state management
- **BlockingCollection** - Command queue with backpressure

## Features Implemented

### Server Features

**Command Pipeline:**
- Multi-stage processing: Policies -> Parse -> Context Building -> Routing -> Execution
- First-pass policies (authentication, rate limiting)
- Second-pass policies (player conditions, muted players)
- Extensible handler registration

**Authentication:**
- Session-based authentication with token generation
- Login/Register commands
- Session validation on every command
- In-memory account storage

**Game World:**
- Room-based navigation (10+ rooms)
- Directional movement (N, S, E, W, NE, NW, SE, SW, Up, Down)
- Room descriptions with exits
- Player location tracking

**Chat System:**
- Broadcast messages to room occupants
- Player muting capability
- Chat event logging

**Server Lifecycle:**
- State machine: LOADING -> ACTIVE -> MAINTENANCE -> SHUTTING_DOWN
- Graceful shutdown with cancellation tokens
- TCP listener start/stop control
- Admin-controllable state transitions

**Networking:**
- Custom binary protocol (header + JSON body + CRC32)
- Packet size validation (max 1MB)
- Per-connection worker threads
- Connection pooling
- Packet logging for debugging

### Client Features

**User Interface:**
- Connection management (server address/port configuration)
- Command input with history (up/down arrow navigation)
- Color-coded message display (chat, events, errors, responses)
- Quick action buttons (movement pad, common commands)
- Image rendering (binary JPEG transfer support)
- Scrollable output window

**Message Handling:**
- Handler-based message routing by type
- Event-driven UI updates
- Session token management
- Auto-reconnect support

### Admin Features (Server GUI)

**Dashboard Panels:**
- Server status (uptime, state, listener status)
- Active players list with mute/kick controls
- Server state controls (ACTIVE, MAINTENANCE, SHUTTING_DOWN buttons)
- Real-time event log with filtering
- Connection statistics

**Server Control:**
- Toggle TCP listener on/off
- Change server operational state
- View packet logs
- Monitor player connections

## Protocol Specification

### Transport Envelope Format

```csharp
{
    MessageId: Guid
    ConnId: ConnectionId
    SessionToken: SessionId?
    CorrelationId: MessageId?
    MessageType: TransportMessageType
    Flags: MessageFlags
    TimestampUtc: DateTime
    Payload: byte[]
}
```

### Wire Protocol (Binary)

```
[Header: 10 bytes]
  - Magic Number: 4 bytes
  - Version: 1 byte
  - Flags: 1 byte
  - Body Length: 4 bytes
[Body: Variable]
  - TransportEnvelope JSON
[CRC32: 4 bytes]
  - Integrity checksum
```

### Command Format (JSON)

```json
{
    "verb": "say",
    "args": ["Hello", "World"]
}
```

### Supported Commands

- `login [username] [password]` - Authenticate
- `register [username] [password]` - Create account
- `say [message]` - Send chat message
- `move [direction]` - Navigate rooms
- `north/south/east/west` - Directional shortcuts
- `northeast/northwest/southeast/southwest` - Diagonal movement
- `up/down` - Vertical movement
- `look` - Describe current room
- `status` - View player information
- `who` - List online players
- `serverstate [active|maintenance|shutdown]` - Change server state (admin)
- `logout` - Disconnect session

## Event Bus Architecture

The system uses a centralized event bus with 15+ typed channels for loose coupling:

- **Authentication** - Login/logout events
- **System** - Server lifecycle and state changes
- **Network** - Connection management (server-side)
- **ClientNetwork** - Connection management (client-side)
- **Protocol** - Protocol-level processing
- **Command** - Command parsing and execution
- **Domain** - Game-world events
- **World** - World state changes
- **Player** - Player-specific events
- **Chat** - Chat messages
- **Error** - Error reporting
- **PacketLog** - Packet transmission logging
- **Log** - Structured application logging
- **GUI** - UI updates
- **Persistence** - Database operations

## Project Structure

```
Solution Root
├── Server.Core/          Core server logic
│   ├── Application/      SystemInitializer (bootstrapping)
│   ├── CommandPipeline/  Orchestrator, handlers, policies, parsers
│   ├── Domain/           Services (chat, movement, player query)
│   ├── Infrastructure/   Lifecycle, identity generators
│   ├── Network/          Supervisor, listener, worker, protocol
│   └── Persistence/      In-memory repositories
├── Server.GUI/           WinUI 3 admin dashboard
├── Client.Core/          Client networking and message routing
├── Client.GUI/           WinUI 3 game client
├── Shared/               Common components
│   ├── Domain/           PlayerState, RoomState, WorldState
│   ├── EventBus/         BasicEventBus, domain events
│   ├── Identity/         Strong-typed IDs
│   ├── Logging/          PacketLogger, FileLogger, EventBusLogger
│   └── Protocol/         TransportEnvelope, MuddyPacket, serialization
├── Server.Tests/         Server unit/integration tests
└── Client.Tests/         Client unit tests
```

## Design Patterns Used

- **Pipeline Pattern** - Command processing flow
- **Strategy Pattern** - Pluggable handlers and policies
- **Observer Pattern** - EventBus pub/sub
- **Repository Pattern** - Data access abstraction
- **Factory Pattern** - Packet creation
- **Command Pattern** - ICommandHandler interface
- **State Pattern** - Server lifecycle states
- **Mediator Pattern** - EventBus as central mediator

## Getting Started

### Prerequisites

- .NET 10 SDK
- Visual Studio 2026 (or VS Code with C# extension)
- Windows 10/11 (for WinUI 3)
- Graphviz (optional, for Doxygen documentation)

### Building

```powershell
# Clone repository
git clone https://github.com/Macroger/COMP72070-26W-Section1-Group3
cd "Project IV"

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test
```

### Running the Server

1. Set `Server.GUI` as startup project
2. Press F5 or click "Start"
3. Server dashboard will open
4. Click "Toggle Listener" to start accepting connections
5. Default port: 5000 (configurable in code)

### Running the Client

1. Set `Client.GUI` as startup project
2. Press F5 or click "Start"
3. Enter server address (default: 127.0.0.1)
4. Enter port (default: 5000)
5. Click "Connect"
6. Use `register` or `login` commands to authenticate
7. Explore the game world with movement commands

### Multiple Clients

Run multiple instances of Client.GUI to test multiplayer functionality. Each client needs its own session.

## Testing

### Unit Tests

```powershell
# Run all tests
dotnet test

# Run specific test project
dotnet test Server.Tests
dotnet test Client.Tests
```

### Test Coverage

- Command pipeline processing
- Authentication flow
- Movement validation
- Chat broadcasting
- Server state transitions
- Network packet serialization
- Policy enforcement

## Documentation

### Doxygen

Generate code documentation:

```powershell
# Install Doxygen (if not installed)
choco install doxygen.install graphviz

# Generate docs
doxygen Doxyfile

# Open documentation
start docs/doxygen/html/index.html
```

### Code Documentation

The codebase includes comprehensive inline documentation and XML comments for all public APIs. Use Visual Studio IntelliSense or generate Doxygen documentation to explore the architecture.

## Logging

### Packet Logs

All network packets are logged to timestamped files:
- `server_packets_YYYY-MM-DD_HH-mm-ss.log`
- `client_packets_YYYY-MM-DD_HH-mm-ss.log`

Format: `[timestamp] direction | Data: { envelope details }`

### Event Logs

Server GUI displays real-time event log with filtering by channel.

## Performance Characteristics

- **Async I/O** - Zero blocking operations
- **Lock-free operations** - ConcurrentDictionary for hot paths
- **Bounded resources** - Max packet size limits, optional queue capacity
- **Memory efficient** - Structs for IDs, value semantics
- **Scalable** - No thread-per-connection model

## Security Features

- Session-based authentication
- Token validation on every command
- Input validation (JSON schema, command arguments)
- Packet integrity checks (CRC32)
- Policy-based authorization
- Server state enforcement (maintenance mode)

## Known Limitations

- In-memory persistence (data lost on server restart)
- No database integration
- No encryption (plaintext TCP)
- No load balancing (single server instance)
- No item/inventory system (planned)
- No combat system (planned)
- No NPC AI (planned)

## Future Enhancements

**Planned Features:**
- Database persistence (SQL Server/PostgreSQL)
- Item and inventory system
- Combat mechanics
- NPC AI and quests
- Skill/leveling system
- OAuth authentication
- TLS/SSL encryption
- Server clustering
- Admin role-based permissions

**Technical Improvements:**
- Performance metrics (Prometheus)
- Telemetry (OpenTelemetry)
- Docker containerization
- Kubernetes deployment
- CI/CD pipeline (GitHub Actions)
- Code coverage reporting

## Contributing

This is an internal student project for COMP72070 (Project IV). Contributions are limited to team members:

- Follow existing code style and patterns
- Write unit tests for new features
- Use meaningful commit messages
- Create feature branches for development

## License

Educational project - Conestoga College BCS Program

## Authors

- Student project for COMP72070-26W-Section1-Group3
- Instructor: [Professor Name]
- Semester: Winter 2026

## Acknowledgments

- .NET team for excellent async/await primitives
- WinUI 3 team for modern UI framework
- Community ToolKit for WinUI controls
- Doxygen for documentation generation

