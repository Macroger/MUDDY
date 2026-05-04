# MUDDY Coding Style Guide

This document outlines the coding conventions used throughout the MUDDY project. Contributors should follow these guidelines to maintain consistency and code quality.

---

## Language & Frameworks

- **C# Version:** 14.0 (or later)
- **Target Framework:** .NET 10
- **IDE:** Visual Studio 2026+ (recommended)

---

## Class Design

### Sealed Classes

Use `sealed` on concrete classes, especially command handlers and services:

```csharp
public sealed class ChatCommandHandler : ICommandHandler
{
    // Implementation
}
```

This prevents accidental inheritance and signals that extensibility happens through interfaces.

### Access Modifiers

- **Classes**: `public` (for public APIs) or `internal` (for infrastructure)
- **Fields**: `private readonly` (immutability enforced through DI)
- **Methods**: `public` (interface methods) or `private` (helpers)
- **Properties**: Avoid mutable state; use `init` or `readonly` where possible

### Naming Conventions

- **Private Fields**: `_camelCase` with leading underscore
- **Public Properties**: `PascalCase`
- **Local Variables**: `camelCase`
- **Constants**: `PascalCase`
- **Methods**: `PascalCase`

---

## Null Handling & Validation

### Early Exit Pattern

Validate and return early:

```csharp
if (context.PlayerState is null || context.WorldState is null)
{
    return new CommandResult { Success = false, Message = "Session state is invalid." };
}
```

### Null Coalescing

Use `??` for safe defaults:

```csharp
string direction = context.Command.Arguments.FirstOrDefault() ?? string.Empty;
```

### Null Pattern Matching

Prefer pattern matching over `== null`:

```csharp
// Good
if (value is null) { }

// Avoid
if (value == null) { }
```

---

## Constructors & Dependency Injection

### Constructor Pattern

- **Private fields for dependencies** — never modify after construction
- **Validation in constructor** — throw `ArgumentNullException` for null dependencies
- **Single responsibility** — keep constructors focused on dependency assignment

```csharp
public ChatCommandHandler(IChatService chatService)
{
    if (chatService == null)
        throw new ArgumentNullException(nameof(chatService), "Chat service cannot be null");
    _chatService = chatService;
}
```

---

## Methods & Async Patterns

### Async Methods

- Return `Task<T>` for async operations
- Use `await` properly; avoid `Task.FromResult()` unless necessary
- Async handler pattern:

```csharp
public async Task<CommandResult> ExecuteAsync(CommandContext context)
{
    // Validation first
    if (context.PlayerState is null || context.WorldState is null)
        return new CommandResult { Success = false, Message = "Invalid state." };

    // Business logic
    return await _service.DoSomethingAsync(context);
}
```

### Synchronous Methods

For truly synchronous operations, wrap in `Task.FromResult()` if interface requires `Task`:

```csharp
return Task.FromResult(_queryService.LookAtRoom(context.PlayerState, context.WorldState));
```

---

## Comments & Documentation

### XML Documentation

**Required** for public classes and methods:

```csharp
/// <summary>
/// Handles chat-related commands: "say".
/// Delegates all logic to <see cref="IChatService"/>.
/// </summary>
public sealed class ChatCommandHandler : ICommandHandler
{
    /// <summary>
    /// Initializes a new instance of <see cref="ChatCommandHandler"/>.
    /// </summary>
    /// <param name="chatService">The domain service that handles chat messages.</param>
    public ChatCommandHandler(IChatService chatService)
    {
        _chatService = chatService;
    }
}
```

### Inline Comments

Use inline comments to explain **non-obvious logic**:

```csharp
// Defensive copy - prevents external modifications to the body after the packet is constructed.
this.Body = body.ToArray();

// The verb IS the direction, so move the player in that direction
return await _movementService.MovePlayerAsync(context.PlayerState, context.WorldState, verb);
```

**Avoid redundant comments:**

```csharp
// Bad - comment repeats code
i++; // Increment i

// Good - comment explains why
i++; // Skip first element due to header byte
```

---

## Code Organization

### Method Order

1. Public static methods
2. Public instance methods (constructors first)
3. Private methods

### Field Initialization

- Initialize fields in constructor via DI
- Use `readonly` to enforce immutability
- Defensive copy mutable arguments:

```csharp
public MuddyPacket(byte[] body, ...)
{
    this.Body = body.ToArray(); // Defensive copy
}
```

---

## Dictionary & Collection Patterns

### Dictionary Initialization

Use modern collection initializers:

```csharp
private readonly Dictionary<string, Func<CommandContext, Task<CommandResult>>> _commands = new()
{
    ["say"] = HandleSay,
    ["look"] = HandleLook,
};
```

### Case-Insensitive Dictionaries

When appropriate:

```csharp
_handlers = new Dictionary<string, ICommandHandler>(StringComparer.OrdinalIgnoreCase);
```

---

## Error Handling

### CommandResult Pattern

Return structured results; don't throw for expected failures:

```csharp
if (string.IsNullOrWhiteSpace(message))
{
    return new CommandResult { Success = false, Message = "Say what?" };
}
```

### Exceptions

Reserve exceptions for:

- **Null dependencies** in constructors
- **Truly unexpected errors** (not control flow)
- **Configuration issues** at startup

---

## Formatting & Style

### Line Length

- Aim for **< 120 characters** per line
- Break long method chains or parameters

### Spacing

- One blank line between methods
- One blank line between logical sections
- No extra blank lines inside methods

### Braces

**Allman style** (opening brace on new line):

```csharp
if (condition)
{
    DoSomething();
}
```

---

## Code Files

### Copyright Header

All `.cs` files must include:

```
// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
```

### Using Statements

- Alphabetical order
- System namespaces first
- One blank line before namespace

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Server.Core.Domain.Services;
```

---

## Testing

### Test Naming

- `ClassName_MethodName_ExpectedBehavior`
- Example: `ChatCommandHandler_ExecuteAsync_ReturnsSuccessWhenValidMessageGiven`

### Assertions

- One assertion per test (when possible)
- Clear failure messages

---

## Command Handler Architecture

### Handler Design

Command handlers are organized by **category** (e.g., Chat, Movement, Player Info), not individual commands. Each handler implements `ICommandHandler` and contains multiple related commands.

```csharp
public sealed class MovementCommandHandler : ICommandHandler
{
    private readonly IWorldMovementService _movementService;
    private readonly IWorldQueryService _queryService;

    public async Task<CommandResult> ExecuteAsync(CommandContext context)
    {
        // Validation first
        if (context.PlayerState is null || context.WorldState is null)
            return new CommandResult { Success = false, Message = "Session state is invalid." };

        string verb = context.Command.CommandType.ToLowerInvariant();

        // Route to appropriate command logic
        // ...
    }
}
```

### Internal Command Routing

For handlers managing multiple commands, use an internal command dictionary for cleaner logic:

```csharp
private readonly Dictionary<string, Func<CommandContext, Task<CommandResult>>> _commands = new()
{
    ["move"] = HandleMove,
    ["go"] = HandleMove,
    ["look"] = HandleLook,
};

if (_commands.TryGetValue(verb, out var handler))
    return await handler(context);
```

---

## Dependency Injection

- Use constructor injection exclusively
- Services are resolved at application startup in `SystemInitializer`
- All handlers registered with `StandardCommandRouter` during initialization
- Prefer interface types in constructor parameters

---

## When in Doubt

1. **Look at existing code** — follow the pattern you see most
2. **Prefer explicit over clever** — readability over brevity
3. **Immutability over mutability** — use `readonly`, `sealed`, and `init`
4. **Document the why, not the what** — code shows what it does, comments explain why
5. **Ask in issues** — this is a learning project; questions are welcome!
