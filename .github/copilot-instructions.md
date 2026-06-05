# GitHub Copilot Instructions — Project IV (MUDDY)

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

### Event Bus
- All events are `sealed record` types nested inside a container class (e.g., `NetworkEvents`, `PlayerEvents`).
- Events are published and subscribed using strongly-typed `Publish<T>` / `Subscribe<T>` methods with an `EventMessageType` category.
- `SubscribeAll(Action<object>)` is reserved exclusively for cross-cutting observers such as loggers. Do not use it for domain subscribers.
- `SubscribeToCategory(EventMessageType, Action<object>)` is reserved for per-subsystem audit observers. Do not use it for domain subscribers.
- GUI components must never use reflection on event objects — use typed `Subscribe<T>` only, with hand-written display strings per event type.

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
