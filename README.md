# COMP72070-26W-Section1-Group3

Repo for the group project, MUDDY - a Multi-User Dungeon using modern UI and networking frameworks.

# MUDDY

MUDDY is a multiplayer, text‑based role-playing game, based on Multi-User Dungeons (MUDs). It is built with a custom C# client–server architecture; the server owns all game logic and world state. Clients act as thin UIs that send/receive structured commands over a lightweight TCP protocol.

## Overview

Genre: Text‑based multiplayer adventure (MUD)
Architecture: C# server + C# client, message‑driven over TCP
Goal: Deliver a modular, testable game platform with clear domains (accounts, characters, rooms, combat, inventory, chat, admin)

### Architecture at a Glance

- Networking/Transport: Custom packet format (fixed header, JSON body, CRC), framing, and backpressure; simple rate limiting and idle handling.
- Messaging: Verb–noun command grammar, per‑command DTOs, centralized message type registry, router → domain modules.
- Policies: Server‑state and authentication policies gate command execution.

# Key Features (MVP Scope)

- Accounts & Login: Create accounts, authenticate, and manage sessions.
- Characters: Create/select characters; view core stats.
- Exploration: Move between rooms; discover occupants, exits, and items.
- Items & Inventory: Pick up, use, and equip items.
- Combat & Spells: Initiate encounters; offensive/healing actions; view results.
- Chat: Player‑to‑player messaging with basic moderation.
- Admin Tools: Kick/mute players, view logs, manage roles.
- Operations: Start/listen, track connected players, handle clean disconnects.

## Technical Highlights

- Server‑driven gameplay: All authoritative logic runs on the server.
- DTO‑based commands: Typed request/response contracts for clarity, validation, and versioning.
- Testability: Clear seams (framer, router, policies, domain methods) for unit and integration tests.
- Extensibility: No transport changes required for new commands.

# Contributing

Internal student project—PRs from team members only. Use the issue templates for new user stories and tasks; follow the domain‑based epic structure for organization.
