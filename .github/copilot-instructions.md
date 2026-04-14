# Copilot Instructions

## Project Guidelines
- Prefers XML '///' style comments for documentation.

## Server Architecture
- Implement a two-pass policy pipeline with pre-parse authentication and post-parse player-state validation.
- The router receives `ParsedCommand` (not `CommandContext`) and returns `ICommandHandler`.
- All responses use nullable `SessionId` (null for errors). 
- Route `SessionId.Unauthenticated` to `AuthenticationPipeline`.
- Use `ConcurrentDictionary` for all repositories in the in-memory v1 server.
- Error responses should be structured as `TransportEnvelope` with payload from `Encoding.UTF8.GetBytes(message)`.

## State Models
- Use immutable state models for game design:
  - `PlayerState`: Contains `ConnId`, `PlayerName`, `CurrentLocation` (RoomId), and `ActiveConditions` (HashSet).
  - `WorldState`: Comprises `IReadOnlyDictionary` of rooms and `IReadOnlySet` of global conditions.
  - `RoomState`: Includes `RoomId`, `Description`, `Conditions`, and `PlayersPresent`.
- Implement init-only properties where applicable for all state models.
- Use identity wrapper pattern for identifiers: `ConnectionId`, `MessageId`, `SessionId`, and `RoomId` should all be readonly structs.
  - `SessionId`: Guid-based identifier for authenticated sessions. `SessionId.Unauthenticated = Guid.Empty` for unauthenticated clients.
  - All identity wrappers must implement `IEquatable<T>` with `==`, `!=` operators.
  - `RoomId` validates non-null/non-empty strings.
  - Use nullable `SessionId?` for `TransportEnvelope.SessionToken`.
- Replace primitive types with the above-defined types throughout the codebase.