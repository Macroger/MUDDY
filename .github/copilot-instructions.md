# GitHub Copilot Instructions — Project IV (MUDDY)

## Core
- Prefer symmetry between client and server core architecture.
- Prefer sharing code between client and server where practical, but split types when responsibilities diverge.
- Optimize instructions for maximum understanding per character.
- Prefer precise edits over full-file regeneration; return only changed code when practical.
- For snippets, include placement context (line numbers, class/method name, or insertion point).
- After edits, summarize what changed, why, and key decisions, tradeoffs, and alternatives.
- Lean memory usage: do not add small/low-impact items to memory; only store durable, high-value preferences or explicitly requested memories.

## Style
- Prefer explicit, readable code over terse code.
- Assign explicit default field values.
- Use explicit access modifiers on all members.
- Avoid expression-bodied members when block bodies are clearer.
- Use `var` only when the RHS type is obvious.
- Use named arguments when intent is unclear.
- Always use braces for `if`, `for`, `foreach`, and `while`.
- Naming: fields `_camelCase`; properties/methods/types/constants `PascalCase`; locals/parameters `camelCase`; interfaces `IPascalCase`.
- All `public` and `internal` members require XML docs (`<summary>`, `<param>`, `<returns>` as applicable).
- Use Doxygen-compatible file header comments (with tags like `@file`, `@namespace`, `@brief`, and optional `@details`) for source files.

## Events
- Default: use event bus for domain/cross-subsystem communication.
- Domain events are nested `sealed record` types in container classes (for example `NetworkEvents`, `PlayerEvents`).
- Use typed `Publish<T>` / `Subscribe<T>` with `EventMessageType`.
- `SubscribeAll(Action<object>)` is for cross-cutting observers only (for example logging).
- `SubscribeToCategory(EventMessageType, Action<object>)` is for subsystem audit observers only.
- Use C# events (`EventHandler` / `EventHandler<T>`) only for tightly-coupled internal signaling with 1–2 known subscribers.
- Supervisor/owner translates infrastructure C# events to domain event-bus events at subsystem boundaries.
- If C# event usage spreads across subsystems or many independent subscribers, migrate to event bus.

## Lifecycle
- Never discard `ISubscriptionToken`.
- Store tokens as fields and dispose them in the owner `Dispose()`.
- Components that subscribe to the bus must implement `IDisposable`.
- `BasicEventBus` must guard `Publish`, `Subscribe`, `SubscribeAll`, and `SubscribeToCategory` when disposed.

## GUI
- Use WinUI 3 APIs and XAML only.
- Do not use UWP (`Windows.UI.Xaml`) or WPF (`System.Windows`) APIs/syntax.
- GUI components must not use reflection-based event inspection; use typed subscriptions and explicit display mapping.
- UI updates must use `DispatcherQueue.TryEnqueue`.

## File Header
- Every new file starts with a header block.
- Required fields: `@file`, `@namespace`, `@brief`.
- Optional field: `@details` only for non-obvious context.
- Do not include author metadata.

## Baseline
- Language: C# 14
- Target framework: .NET 10
- GUI framework: WinUI 3
