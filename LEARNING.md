# MUDDY Learning Guide

Welcome! This document explains how MUDDY works and how it's structured. Whether you're learning MUD development, .NET architecture, or just curious about how the project is organized, this guide will help you understand the "why" behind the design decisions.

---

## Table of Contents

1. [What is a MUD?](#what-is-a-mud)
2. [Architecture Overview](#architecture-overview)
3. [Core Concepts](#core-concepts)
4. [How Commands Work](#how-commands-work)
5. [How to Add a New Command](#how-to-add-a-new-command)
6. [Design Patterns](#design-patterns)
7. [Project Structure](#project-structure)
8. [Key Design Decisions](#key-design-decisions)

---

## What is a MUD?

### Brief History

A **MUD (Multi-User Dungeon)** is a text-based multiplayer game. Players interact with a virtual world entirely through text commands and descriptions. MUDs emerged in the 1970s-80s and were a precursor to modern MMORPGs.

### Core Gameplay Loop

```
Player Input → Game Processes → Text Output → Repeat
"go north"  → Update location → "You enter a forest..." → Waiting for next command
```

### MUDDY's Scope

MUDDY is a **learning template**, not a full game. It demonstrates:
- How commands are parsed and routed
- How player state is managed
- How world state is maintained
- How services handle game logic
- Professional .NET architecture patterns

---

## Architecture Overview

### High-Level System Design

MUDDY uses a **layered, client-server architecture** with clear separation of concerns:

```
          CLIENT SIDE                                  SERVER SIDE

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  ┌──────────────────────────┐                  ┌─────────────────────────┐  │
│  │    GUI / User Interface  │                  │   Command Pipeline      │  │
│  │  (Terminal/Console UI)   │                  │ (Parsing, validation)   │  │
│  └──────────────────────────┘                  └─────────────────────────┘  │
│           ▲                                             ▲                   │
│           │                                             │                   │
│  ┌────────▼─────────────────────────────────────────────▼────────────────┐  │
│  │                    NETWORKING LAYER                                   │  │
│  │         (Muddy Protocol, TCP/IP, Message Serialization)               │  │
│  └────────┬─────────────────────────────────────────────┬────────────────┘  │
│           │                                             │                   │
│  ┌────────▼──────────────────┐           ┌──────────────▼──────────────┐    │
│  │  Client.Core              │           │  Command Handlers           │    │
│  │  Command Pipeline         │           │  (Category-based routing)   │    │
│  │  (Input buffering, etc)   │           └──────────────┬──────────────┘    │
│  └───────────────────────────┘                          │                   │
│                                                         ▼                   │
│                                          ┌──────────────────────────────┐   │
│                                          │  Domain Services             │   │
│                                          │  (Game logic)                │   │
│                                          └──────────────┬───────────────┘   │
│                                                         │                   │
│                                          ┌──────────────▼───────────────┐   │
│                                          │  Repositories & State        │   │
│                                          │  (Data access, persistence)  │   │
│                                          └──────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Data Flow: User Input to Response

```
1. Player types: "say hello everyone"
   ↓
2. CommandPipelineOrchestrator parses into:
   - CommandType: "say"
   - Arguments: ["hello", "everyone"]
   ↓
3. StandardCommandRouter finds matching handler:
   - Looks up "say" → ChatCommandHandler
   ↓
4. ChatCommandHandler executes:
   - Validates player state
   - Calls ChatService.BroadcastMessageAsync()
   ↓
5. ChatService broadcasts to all players in room
   ↓
6. CommandResult returned to player
```

### Key Components

| Layer | Component | Responsibility |
|-------|-----------|-----------------|
| **GUI** | Terminal UI / Console Output | Displays game text to player, accepts input |
| **Networking** | Muddy Protocol Handler | Serializes/deserializes messages, TCP/IP transport |
| **Client Pipeline** | ClientCommandPipelineOrchestrator | Buffers input, sends to server |
| **Server Pipeline** | CommandPipelineOrchestrator | Parses commands into structured format |
| **Router** | StandardCommandRouter | Routes verbs to appropriate handlers |
| **Handlers** | Command Handler Classes | Execute category-specific logic |
| **Services** | Domain Services | Implement core game mechanics |
| **Data** | Repositories & State | Manage persistence and state snapshots |
| **Events** | Event Bus | Publishes domain events for real-time updates |

### Layer Responsibilities

**GUI / User Interface**
- Renders text output to the player
- Captures player input from keyboard/terminal
- Does NOT contain game logic (read-only view)

**Networking**
- Sends/receives Muddy protocol packets over TCP/IP
- Handles serialization and deserialization
- Manages connection state, message ordering, ACKs
- Client and server both have networking components

**Client.Core Pipeline**
- Buffers player input
- Sends structured commands to server
- Receives and displays responses

**Server Command Pipeline** (previously shown as just "Command Pipeline")
- Parses raw text input into structured `ParsedCommand`
- Enriches context with player/world state
- Passes to router

**StandardCommandRouter**
- Looks up handler by verb
- Orchestrates handler execution

**Command Handlers**
- Entry point for command execution
- Validates input for handler-specific rules
- Delegates to domain services

**Domain Services**
- Pure game logic (can be tested independently)
- No knowledge of commands or networking
- Examples: ChatService, WorldMovementService

**Repositories & State**
- Player and World data access
- Persistence (loading/saving)
- In-memory state snapshots

---

## Core Concepts

### Commands

A **command** is an action a player performs. Examples:
- `say hello` — Chat with nearby players
- `move north` — Travel to adjacent room
- `look` — Examine current room
- `status` — Check player info

Commands are **case-insensitive** and follow the pattern:
```
[verb] [arguments...]
```

### Player State

Represents a single player at a moment in time:
```csharp
class PlayerState
{
    PlayerId Id { get; }           // Unique identifier
    string Name { get; }           // Player's name
    RoomId CurrentRoomId { get; }  // Where they are
    // ... other properties
}
```

### World State

The current snapshot of the game world:
```csharp
class WorldState
{
    Dictionary<RoomId, RoomState> Rooms { get; }     // All rooms
    HashSet<ActiveWorldConditions> Conditions { get; } // Global effects
}
```

**Important:** World state is immutable during a turn. Services return modified state; the system applies changes atomically.

### Command Result

All commands return a standardized result:
```csharp
class CommandResult
{
    bool Success { get; }       // Did it work?
    string Message { get; }     // Feedback to player
    // ... other metadata
}
```

This replaces exceptions for expected failures (invalid input, game logic blocks, etc.).

### Domain Services

Services encapsulate game logic independent of command handling:
- `ChatService` — Broadcasting messages
- `WorldMovementService` — Moving players between rooms
- `PlayerQueryService` — Retrieving player information
- `WorldQueryService` — Describing the world

Services **never know about commands** — they work with domain objects directly.

---

## How Commands Work

### The Handler Pattern

Command handlers are organized by **category**, not individual commands:

**Bad approach (don't do this):**
```
SayCommandHandler
LookCommandHandler
MoveCommandHandler
GoCommandHandler
NorthCommandHandler
```

**Good approach (MUDDY does this):**
```
ChatCommandHandler        (handles: say)
MovementCommandHandler    (handles: move, go, look, north, south, ...)
PlayerCommandHandler      (handles: status, who, player)
```

### Handler Structure

Every handler implements `ICommandHandler`:

```csharp
public interface ICommandHandler
{
    Task<CommandResult> ExecuteAsync(CommandContext context);
}
```

Example: `ChatCommandHandler`

```csharp
public sealed class ChatCommandHandler : ICommandHandler
{
    private readonly IChatService _chatService;

    public ChatCommandHandler(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<CommandResult> ExecuteAsync(CommandContext context)
    {
        // 1. Validate
        if (context.PlayerState is null || context.WorldState is null)
            return new CommandResult { Success = false, Message = "Invalid state." };

        // 2. Extract command data
        string verb = context.Command.CommandType.ToLowerInvariant();
        string message = string.Join(" ", context.Command.Arguments);

        // 3. Route to logic
        if (verb == "say")
        {
            if (string.IsNullOrWhiteSpace(message))
                return new CommandResult { Success = false, Message = "Say what?" };

            return await _chatService.BroadcastMessageAsync(context.PlayerState, context.WorldState, message);
        }

        return new CommandResult { Success = false, Message = $"Unknown command: {verb}" };
    }
}
```

### Handler Registration

All handlers are registered at startup in `SystemInitializer`:

```csharp
var cmdRouter = new StandardCommandRouter();
cmdRouter.RegisterHandler("say", new ChatCommandHandler(_chatService));
cmdRouter.RegisterHandler("move", new MovementCommandHandler(_movementService, _queryService));
cmdRouter.RegisterHandler("look", new MovementCommandHandler(_movementService, _queryService));
// ... more registrations
```

---

## How to Add a New Command

Let's walk through adding a new `"emote"` command that lets players express emotions (e.g., `emote shivers nervously`).

### Step 1: Define the Domain Service

First, create the business logic independent of commands:

**File:** `Server.Core/Domain/Services/EmoteService/IEmoteService.cs`

```csharp
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;

namespace Server.Core.Domain.Services.EmoteService
{
    /// <summary>
    /// Handles emote messages (player expressions).
    /// </summary>
    public interface IEmoteService
    {
        /// <summary>
        /// Broadcasts an emote to all players in the same room.
        /// </summary>
        Task<CommandResult> BroadcastEmoteAsync(PlayerState player, WorldState world, string emoteText);
    }
}
```

**File:** `Server.Core/Domain/Services/EmoteService/EmoteService.cs`

```csharp
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;
using Server.Core.EventBus.DomainEvents;
using Shared.EventBus;

namespace Server.Core.Domain.Services.EmoteService
{
    /// <summary>
    /// Implementation of emote broadcasting service.
    /// </summary>
    public sealed class EmoteService : IEmoteService
    {
        private readonly IEventBus _eventBus;

        public EmoteService(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task<CommandResult> BroadcastEmoteAsync(PlayerState player, WorldState world, string emoteText)
        {
            // Validation
            if (player is null || world is null)
                return new CommandResult { Success = false, Message = "Invalid state." };

            if (string.IsNullOrWhiteSpace(emoteText))
                return new CommandResult { Success = false, Message = "Emote what?" };

            // Broadcast to room
            var emoteEvent = new PlayerEmotedEvent(player.Id, player.CurrentRoomId, emoteText);
            await _eventBus.PublishAsync(emoteEvent);

            return new CommandResult { Success = true, Message = "You emoted." };
        }
    }
}
```

### Step 2: Create the Command Handler

**File:** `Server.Core/CommandPipeline/CommandHandler/EmoteCommandHandler.cs`

```csharp
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.Services.EmoteService;

namespace Server.Core.CommandPipeline.CommandHandler
{
    /// <summary>
    /// Handles emote commands: "emote".
    /// Delegates to <see cref="IEmoteService"/>.
    /// </summary>
    public sealed class EmoteCommandHandler : ICommandHandler
    {
        private readonly IEmoteService _emoteService;

        public EmoteCommandHandler(IEmoteService emoteService)
        {
            _emoteService = emoteService;
        }

        public async Task<CommandResult> ExecuteAsync(CommandContext context)
        {
            if (context.PlayerState is null || context.WorldState is null)
                return new CommandResult { Success = false, Message = "Session state is invalid." };

            string message = string.Join(" ", context.Command.Arguments);
            return await _emoteService.BroadcastEmoteAsync(context.PlayerState, context.WorldState, message);
        }
    }
}
```

### Step 3: Register the Handler

In `SystemInitializer.cs`, add the handler registration:

```csharp
// Create domain services
_emoteService = new EmoteService(_eventBus);

// Create handlers
var emoteHandler = new EmoteCommandHandler(_emoteService);

// Register handlers
cmdRouter.RegisterHandler("emote", emoteHandler);
```

### Step 4: Write Tests

**File:** `Server.Tests/CommandPipeline/EmoteCommandHandlerTests.cs`

```csharp
using Server.Core.CommandPipeline.CommandHandler;
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.Services.EmoteService;
using Xunit;

namespace Server.Tests.CommandPipeline
{
    public class EmoteCommandHandler_Tests
    {
        private readonly EmoteCommandHandler _handler;
        private readonly MockEmoteService _emoteService;

        public EmoteCommandHandler_Tests()
        {
            _emoteService = new MockEmoteService();
            _handler = new EmoteCommandHandler(_emoteService);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsFailure_WhenPlayerStateIsNull()
        {
            var context = new CommandContext
            {
                PlayerState = null,
                WorldState = new WorldState(new Dictionary<RoomId, RoomState>(), new HashSet<ActiveWorldConditions>()),
                Command = new ParsedCommand { CommandType = "emote", Arguments = new[] { "shivers" } }
            };

            var result = await _handler.ExecuteAsync(context);

            Assert.False(result.Success);
            Assert.Contains("invalid", result.Message.ToLower());
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsSuccess_WhenEmoteIsValid()
        {
            var playerState = new PlayerState { Id = new PlayerId("player1"), Name = "Alice" };
            var worldState = new WorldState(new Dictionary<RoomId, RoomState>(), new HashSet<ActiveWorldConditions>());
            var context = new CommandContext
            {
                PlayerState = playerState,
                WorldState = worldState,
                Command = new ParsedCommand { CommandType = "emote", Arguments = new[] { "shivers", "nervously" } }
            };

            var result = await _handler.ExecuteAsync(context);

            Assert.True(result.Success);
        }
    }
}
```

### Step 5: Test It

```bash
dotnet build
dotnet test
```

Done! Your `emote` command is now part of MUDDY.

---

## Design Patterns

### Command Handler Pattern

Handlers are **stateless, category-based, and injectable**. This allows:
- Easy testing (mock the services)
- Scaling (add handlers without modifying router)
- Clear responsibility (each handler owns its commands)

### Repository Pattern

Repositories abstract data access:

```csharp
// Don't do this
var players = _database.Query("SELECT * FROM players");

// Do this
var players = _playerRepository.GetAllPlayers();
```

Benefits: Testability, flexibility, isolation of concerns.

### Event-Driven Architecture

Domain events decouple subsystems:

```csharp
// Service publishes event
await _eventBus.PublishAsync(new PlayerChatEvent(...));

// Other listeners react
_eventBus.Subscribe<PlayerChatEvent>(async e => {
    // Maybe log it, update UI, etc.
});
```

### Dependency Injection

All dependencies flow through constructors:

```csharp
public ChatCommandHandler(IChatService chatService)
{
    _chatService = chatService; // Injected, not created
}
```

Benefits: Testability, loose coupling, flexibility.

### Immutable State Pattern

State objects (PlayerState, WorldState) are read-only. Services return new state:

```csharp
// Don't modify in-place
player.Health -= 10;

// Return new state
return new PlayerState(...) { Health = player.Health - 10 };
```

Benefits: Predictability, easier concurrency handling, auditability.

---

## Project Structure

```
MUDDY/
│
├── Server.Core/                          # Main server application
│   ├── Application/
│   │   └── SystemInitializer.cs          # Startup, DI registration
│   │
│   ├── CommandPipeline/
│   │   ├── CommandRouter/
│   │   │   └── StandardCommandRouter.cs  # Routes verbs → handlers
│   │   ├── CommandHandler/
│   │   │   ├── ChatCommandHandler.cs
│   │   │   ├── MovementCommandHandler.cs
│   │   │   └── ...
│   │   ├── ContextBuilder/
│   │   │   └── CommandContext.cs         # Context passed to handlers
│   │   └── Types/
│   │       └── CommandResult.cs          # Standardized response
│   │
│   ├── Domain/
│   │   ├── Services/
│   │   │   ├── ChatService/
│   │   │   ├── WorldMovementService/
│   │   │   └── ...
│   │   ├── Repositories/
│   │   │   ├── PlayerRepository.cs
│   │   │   └── WorldRepository.cs
│   │   └── World/
│   │       ├── GameWorldFactory.cs       # Creates default world
│   │       ├── RoomState.cs
│   │       └── PlayerState.cs
│   │
│   └── EventBus/
│       └── DomainEvents/
│           ├── PlayerEvents.cs
│           └── ...
│
├── Shared/                               # Shared between client & server
│   ├── Network/
│   ├── EventBus/
│   └── Protocol/
│
├── Server.Tests/                         # Unit tests
│   ├── CommandPipeline/
│   ├── Domain/
│   └── ...
│
├── Client.Core/                          # Client-side logic
│   └── CommandPipeline/
│
└── [Documentation]
    ├── CONTRIBUTING.md                   # How to contribute
    ├── CODING_STYLE.md                   # Code standards
    ├── LEARNING.md                       # This file
    └── README.md                         # Project overview
```

### What Goes Where?

| Concern | Location | Why |
|---------|----------|-----|
| Business logic | Domain/Services | Independent of UI/commands |
| Command routing | CommandPipeline/CommandRouter | Orchestration layer |
| Command handling | CommandPipeline/CommandHandler | Request entry points |
| Data access | Domain/Repositories | Abstraction of persistence |
| Shared types | Shared/ | Used by client & server |
| Tests | Server.Tests/ | Keep tests organized by target |

---

## Key Design Decisions

### Why Category-Based Handlers Instead of One-per-Command?

**Decision:** Group related commands into single handler classes.

**Rationale:**
- Commands often share validation and context
- Reduces boilerplate (one constructor instead of N)
- Related commands usually call similar services
- Easier to reason about coherent functionality

**Example:**
```
Good: MovementCommandHandler handles {move, go, look, north, south, ...}
Bad:  MoveCommandHandler, GoCommandHandler, LookCommandHandler, NorthCommandHandler, ...
```

### Why CommandResult Instead of Throwing Exceptions?

**Decision:** Return structured CommandResult for all outcomes (success and expected failures).

**Rationale:**
- Player input failures aren't exceptional (e.g., "you can't move that way")
- Exceptions reserved for truly unexpected errors (null dependencies, config issues)
- Easier to test (no try-catch required)
- Clearer intent (return value shows outcome)

**Example:**
```csharp
// Don't do this
if (playerIsTired)
    throw new GameException("Too tired to move");

// Do this
if (playerIsTired)
    return new CommandResult { Success = false, Message = "You're too tired." };
```

### Why Immutable State?

**Decision:** State objects (PlayerState, WorldState) are read-only and returned as new instances.

**Rationale:**
- Predictable behavior (no hidden modifications)
- Easier to reason about (snapshots in time)
- Future-proof for concurrency (multiple players at once)
- Audit trail (can track state changes)

### Why Domain Services Separate from Handlers?

**Decision:** Game logic lives in services, not handlers.

**Rationale:**
- Handlers are about request routing and validation
- Services are about game mechanics (reusable)
- A future REST API could use the same services
- Easier to test services independently
- Forces separation of concerns

---

## Common Patterns to Follow

### When Adding Features

1. **Start with domain logic** — Write the service first, independent of commands
2. **Then add handler** — Wire the service into a command handler
3. **Register in SystemInitializer** — Add to the router
4. **Write tests** — Test both service and handler
5. **Document** — Update LEARNING.md if pattern is new

### When Debugging

1. Check **CommandRouter** — Is the verb registered?
2. Check **CommandHandler** — Does it route to the right service?
3. Check **Domain Service** — Is the business logic correct?
4. Check **Repositories** — Is data being persisted correctly?

### When Testing

1. Mock domain services in handler tests
2. Test services with real repositories or mocks
3. Test edge cases (null input, invalid state, etc.)
4. Use descriptive test names: `Handler_Method_Expectation`

---

## Further Reading

- **[CONTRIBUTING.md](./CONTRIBUTING.md)** — How to contribute changes
- **[CODING_STYLE.md](./CODING_STYLE.md)** — Code conventions
- **Existing handlers** — See `ChatCommandHandler`, `MovementCommandHandler` for examples
- **Domain services** — See `ChatService`, `WorldMovementService` for patterns

---

## Questions?

If something in this guide is unclear:
- Open an [issue on GitHub](https://github.com/Macroger/MUDDY/issues)
- Start a discussion (GitHub Discussions)
- The learning-focused nature means **your questions help improve this guide**!

---

## About This Document

This learning guide was created with the assistance of Claude (Anthropic's AI assistant) under the guidance of Matthew Schatz (Macroger). The content was curated, reviewed, and approved by the project maintainers to ensure accuracy and alignment with MUDDY's educational goals. AI-assisted generation helped organize complex concepts into a learner-friendly format, but all technical decisions and architectural explanations reflect the actual design of the MUDDY project.

