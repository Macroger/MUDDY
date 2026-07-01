# GitHub Copilot Instructions — Project IV (MUDDY)

## General Guidelines
- Prefer symmetrical architecture between client and server core applications. Mirror systems and patterns across domains while specializing them for unique tasks to improve consistency, maintainability, and cognitive load.

## Code Style

### Explicitness over terseness
Always prefer explicit, readable code over compact shorthand. When in doubt, be more verbose — clarity is more important than brevity.

- **Always assign explicit default values**, even when the runtime would assign them automatically:
  ```csharp
  // Preferred
  private bool _disposed = false;
  private int _count = 0;
  private string? _name = null;

  // Avoid
  private bool _disposed;
  private int _count;
  private string? _name;
  ```

- **Always use explicit access modifiers** on all members:
  ```csharp
  // Preferred
  private readonly IEventBus _bus;
  public void Dispose() { }

  // Avoid
  readonly IEventBus _bus;
  void Dispose() { }
  ```

- **Avoid expression-bodied members** when a block body is clearer:
  ```csharp
  // Preferred
  public override int GetHashCode()
  {
	  return _handler.GetHashCode();
  }

  // Avoid (unless trivially obvious)
  public override int GetHashCode() => _handler.GetHashCode();
  ```

- **Avoid implicit `var`** when the type is not immediately obvious from the right-hand side:
  ```csharp
  // Preferred
  Dictionary<EventMessageType, HashSet<Action<object>>> categoryObservers = new();
  ISubscriptionToken token = bus.Subscribe<MyEvent>(EventMessageType.Domain, Handler);

  // Acceptable only when the type is obvious
  var handler = new EventSubscriber<T>(action);
  ```

- **Prefer named parameters** for calls where argument intent is not obvious from context.

- **Do not omit braces** on single-line `if`, `for`, `foreach`, or `while` statements:
  ```csharp
  // Preferred
  if (_disposed)
  {
	  return;
  }

  // Avoid
  if (_disposed) return;
  ```

### Naming
- Fields: `_camelCase` with leading underscore
- Properties and methods: `PascalCase`
- Local variables: `camelCase`
- Constants: `PascalCase`
- Interfaces: `IPascalCase`

### XML Documentation
- All `public` and `internal` members should have XML doc comments (`<summary>`, `<param>`, `<returns>` as applicable).
- Do not omit documentation to save lines.

## Architecture

### Communication Patterns

MUDDY uses two distinct communication patterns based on architectural context. Use the right tool for the job:

#### 1. Domain/Cross-Boundary Events (Event Bus) — Default Pattern

Use for communication that crosses architectural boundaries (subsystem-to-subsystem).

**When to use:**
- Events cross independent subsystems (Network layer → Command Pipeline → Business Logic → Presentation)
- Multiple independent subscribers might listen (logging, metrics, debugging, UI updates)
- Temporal decoupling is beneficial (publisher doesn't wait for subscribers)
- Event replayability or inspection is useful

**Requirements:**
- Always use `sealed record` types nested in container classes (`NetworkEvents`, `PlayerEvents`, etc.)
- Always use `Publish<T>` / `Subscribe<T>` with an `EventMessageType` category
- Must store and dispose `ISubscriptionToken` in owning class's `Dispose()` method
- This is the default pattern — use this unless you have a specific reason not to

**Examples:**
- `NetworkEvents.Packets.PacketReceived` → published by network layer, subscribed by router and logger
- `PlayerEvents.PlayerMoved` → published by game logic, subscribed by UI and statistics tracker
- `AuthenticationEvents.LoginSucceeded` → published by auth system, subscribed by UI and session manager

#### 2. Infrastructure/Tightly-Coupled Events (C# Events) — Exception Pattern

Use for internal component signaling within a single subsystem where tight coupling is by design.

**When to use:**
- Components are **lifecycle-coupled** (creator owns the component for its entire lifetime)
- Communication is **internal to a subsystem** (not crossing architectural boundaries)
- There is **exactly one or two known subscribers** (not "multiple independent" subscribers)
- The pattern is **supervisor → worker** or **controller → view** (directional, not broadcast)

**Requirements:**
- Use standard C# `EventHandler` or `EventHandler<T>` delegates
- Do not mix with event bus — these are internal infrastructure signals only
- Document clearly in XML comments that these are infrastructure events
- The **supervisor/owner** should act as a translation layer, converting infrastructure signals to domain events for the event bus

**Examples:**
- Server: `IConnectionWorker.MessageReceived` → signals packets to `StandardNetworkSupervisor` (infrastructure)
- Client: `IClientConnectionWorker.ConnectionClosed` → signals state to `ClientNetworkSupervisor` (infrastructure)
- UI: Button control `Click` → signals user action to Button click handler (infrastructure)

**Translation Layer Pattern:**

Infrastructure signals should be translated to domain events at the subsystem boundary:

```csharp
// Infrastructure level: C# event from worker
worker.MessageReceived += OnWorkerMessageReceived;

// Translation: C# event → domain event via event bus
private void OnWorkerMessageReceived(object? sender, TransportEnvelope envelope)
{
    // Here we convert infrastructure signal to domain event
    // This happens in the supervisor/boundary component
    _eventBus.Publish(
        EventMessageType.Network,
        new ClientNetworkEvents.Packets.PacketReceived(envelope));
}
```

**Rationale for Exception:**
- Avoids unnecessary event bus overhead on hot paths (every packet would traverse the bus)
- Keeps internal component concerns isolated from domain concerns
- More intuitive and performant for tightly-coupled infrastructure
- Maintains symmetry between client and server implementations
- Supervisor is the perfect place to translate infrastructure signals to domain events

### Domain Events (Event Bus)

- All domain events are `sealed record` types nested inside a container class (e.g., `NetworkEvents`, `PlayerEvents`).
- Events are published and subscribed using strongly-typed `Publish<T>` / `Subscribe<T>` methods with an `EventMessageType` category.
- `SubscribeAll(Action<object>)` is reserved exclusively for cross-cutting observers such as loggers. Do not use it for domain subscribers.
- `SubscribeToCategory(EventMessageType, Action<object>)` is reserved for per-subsystem audit observers. Do not use it for domain subscribers.
- GUI components must never use reflection on event objects — use typed `Subscribe<T>` only, with hand-written display strings per event type.

### Infrastructure Events (C# Events)

When using C# events for infrastructure signaling (see Communication Patterns above):

- Use `EventHandler<T>` for events with payload data; plain `EventHandler` for state-only signals.
- Add XML doc comments indicating these are **infrastructure events** (not domain events) to prevent misuse.
- Never publish infrastructure events to the event bus directly — the supervisor/owner should translate them instead.
- Supervisor is the **only** appropriate place to convert infrastructure signals to domain events via the event bus.
- If you find yourself adding many independent subscribers to a C# event (more than 1-2), that's a signal to convert it to an event bus pattern instead.

### Subscription Tokens

- Every call to `Subscribe`, `SubscribeAll`, or `SubscribeToCategory` returns an `ISubscriptionToken`.
- Tokens must be stored as fields and disposed in the owning class's `Dispose()` method.
- Never discard a subscription token.

### Disposal

- `BasicEventBus` implements `IDisposable`. Check `_disposed` at the start of `Publish`, `Subscribe`, `SubscribeAll`, and `SubscribeToCategory`.
- Components that subscribe to the bus must implement `IDisposable` and dispose their tokens.

## File Headers

Every new file must begin with a file header block. Use `///` triple-slash lines for content (recognised by both Doxygen and C# XML doc tooling) and plain `//` separator lines for the decorative border.

**Required fields:** `@file`, `@namespace`, `@brief`
**Optional field:** `@details` — use for non-obvious design decisions, known limitations, or anything a contributor needs to know before editing the file.
Do **not** include an author field — authorship is tracked by Git.

```csharp
// =============================================================================
/// @file       BasicEventBus.cs
/// @namespace  Shared.EventBus
/// @brief      Core synchronous event bus. Dispatches strongly-typed events
///             to registered subscribers and untyped observers.
/// @details    Implements IDisposable. All subscriber components must store
///             and dispose their ISubscriptionToken on shutdown.
///             Thread-unsafe by design — intended for single-threaded use
///             on the game loop only.
// =============================================================================
```

Omit `@details` when there is nothing non-obvious to say — do not pad it with information already clear from the class name or summary.

## Project Details
- Language: C# 14
- Target framework: .NET 10
- GUI: WinUI 3 (Server.GUI project) — no reflection on UI thread, all UI updates via `DispatcherQueue.TryEnqueue`
- **Only use WinUI 3 XAML syntax and APIs.** Do not use UWP (Windows.UI.Xaml), WPF (System.Windows), or any other Windows framework syntax. Common mistakes to avoid: ResourceDictionary.ThemeDictionaries (UWP-only), {ThemeResource} for custom resources (use {StaticResource} instead), Window.Resources must contain direct resource definitions or merged dictionaries.
