# 🗺️ MUDDY Development Roadmap

Current Version: **0.1.1**  
Last Updated: January 2026

---

## Overview

This roadmap outlines planned improvements and feature additions for MUDDY. Items are prioritized by impact, effort, and strategic importance. Community contributions are welcome for any items marked as `help-wanted`.

### Status Legend
- ⏳ **Planned** — Scheduled for next development cycle
- 🔄 **In Progress** — Currently being developed
- 📋 **Backlog** — Approved but not yet scheduled
- 🚀 **Released** — Completed and deployed
- ❌ **Deferred** — Postponed (see notes)

---

## v0.2 — Core Foundation & Stability
*Focus: Essential infrastructure and improved error handling*

| Priority | Item | Description | Impact | Effort | Complexity | Files | Status |
|:--------:|------|-------------|--------|:------:|:----------:|:------|:------:|
| 🔴 High | SQL Persistence Layer | Replace in-memory storage with SQLite for data persistence across server restarts | Critical | ⭐⭐⭐ | 🟡 Medium | `Persistence/SqlLite/` | ⏳ Planned |
| 🔴 High | Enhanced Error Handling | Implement detailed error messages and error codes in command responses for better debugging | Important | ⭐⭐ | 🟢 Low | `CommandPipeline/Types/` | ⏳ Planned |
| 🔴 High | Room Descriptions Enhancement | Upgrade room descriptions to multi-line, immersive text with atmospheric details and object lists | Quality | ⭐⭐ | 🟢 Low | `Domain/World/` | ⏳ Planned |

---

## v0.3 — Gameplay Expansion
*Focus: New systems and player interaction*

| Priority | Item | Description | Impact | Effort | Complexity | Files | Status |
|:--------:|------|-------------|--------|:------:|:----------:|:------|:------:|
| 🟠 Med-High | Inventory System | Enable players to pick up, drop, and manage items; foundational for trading and quests | Gameplay | ⭐⭐⭐ | 🟡 Medium | `Domain/Items/`, `CommandPipeline/CommandHandler/` | 📋 Backlog |
| 🟠 Med-High | NPC System | Add NPCs to the world with dialogue trees and interaction; enable quest givers and shopkeepers | Gameplay | ⭐⭐⭐ | 🟠 Medium-High | `Domain/Npcs/`, `CommandPipeline/CommandHandler/` | 📋 Backlog |
| 🟡 Medium | Extended Commands | Implement additional commands: `examine`, `take`, `drop`, `give`, `emote`, `help` | Gameplay | ⭐⭐ | 🟢 Low | `CommandPipeline/CommandHandler/` | 📋 Backlog |
| 🟡 Medium | Authentication Enhancements | Add password hashing (bcrypt), token expiry, session refresh, and login rate-limiting | Security | ⭐⭐ | 🟡 Medium | `Persistence/`, `CommandPipeline/Authentication/` | 📋 Backlog |

---

## v0.4 — Polish & Quality
*Focus: Developer experience and maintainability*

| Priority | Item | Description | Impact | Effort | Complexity | Files | Status |
|:--------:|------|-------------|--------|:------:|:----------:|:------|:------:|
| 🟡 Medium | Comprehensive Test Coverage | Add integration tests, end-to-end tests, and edge case coverage | Quality | ⭐⭐ | 🟢 Low-Medium | `*Tests/` | 📋 Backlog |
| 🟡 Medium | Admin Panel Enhancements | Add world reload, room state viewer, NPC management, and item spawning controls | Operations | ⭐⭐ | 🟢 Low-Medium | `Server.GUI/` | 📋 Backlog |
| 🟡 Medium | Logging & Diagnostics | Implement structured logging with levels, performance metrics, and audit trails | Operations | ⭐⭐ | 🟢 Low | `Shared/Logging/` | 📋 Backlog |
| 🟢 Low | Command Help System | Create help command with command discovery, syntax hints, and examples | UX | ⭐ | 🟢 Low | `CommandPipeline/Types/`, `CommandPipeline/CommandHandler/` | 📋 Backlog |

---

## Future / Backlog
*Focus: Major features and long-term vision*

| Priority | Item | Description | Impact | Effort | Complexity | Files | Status |
|:--------:|------|-------------|--------|:------:|:----------:|:------|:------:|
| 🟢 Low | Room Conditions & Environmental Effects | Implement active condition logic (e.g., `Raining` affects movement, visibility) | Gameplay | ⭐⭐ | 🟡 Medium | `CommandPipeline/`, `Domain/World/` | 📋 Backlog |
| 🔵 Future | Guild & Player Grouping | Social/cooperative gameplay: guilds, parties, shared goals | Gameplay | ⭐⭐⭐ | 🟠 High | `Domain/Social/` | 📋 Backlog |
| 🔵 Future | Combat System | Basic combat: attack mechanics, health/mana, spells, NPC combat | Gameplay | ⭐⭐⭐⭐ | 🔴 Very High | `Domain/Combat/` | 📋 Backlog |

---

## Legend

### Priority Levels
- 🔴 **High** — Blocking other features or critical for playability
- 🟠 **Med-High** — Important for gameplay expansion
- 🟡 **Medium** — Valuable additions for quality or feature parity
- 🟢 **Low** — Polish, nice-to-have, or foundational
- 🔵 **Future** — Long-term vision, not yet prioritized

### Effort Estimates
- ⭐ — 1-2 hours
- ⭐⭐ — 4-8 hours
- ⭐⭐⭐ — 1-2 days
- ⭐⭐⭐⭐ — 3+ days

### Complexity Levels
- 🟢 **Low** — Straightforward implementation, minimal dependencies
- 🟡 **Medium** — Some design decisions, moderate scope
- 🟠 **Medium-High** — Complex interactions, careful design needed
- 🔴 **High** — Very complex, significant architectural implications
- 🔴 **Very High** — Major feature with many subsystems

---

## Recommended Implementation Order

For **maximum value + achievable scope**, follow this sequence:

1. **v0.2 Item 3** — Room Descriptions (quick win, immediate UX boost)
2. **v0.2 Item 1** — SQL Persistence (foundation for all future features)
3. **v0.2 Item 2** — Enhanced Error Handling (improves DX)
4. **v0.3 Item 3** — Extended Commands (`examine`, `take`, `drop`, `help`)
5. **v0.3 Item 1** — Inventory System (enables item gameplay)
6. **v0.3 Item 2** — NPC System (ties everything together)

---

## Contributing

Community contributions are welcome! If you'd like to work on any of these items:

1. Check the [GitHub Issues](https://github.com/Macroger/MUDDY/issues) for the corresponding issue
2. Leave a comment to claim the work
3. Follow the [CONTRIBUTING.md](./CONTRIBUTING.md) guidelines
4. Reference this roadmap in your PR description

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 0.1.1 | Jan 2026 | Initial roadmap created with 14 prioritized items |

---

*Last reviewed: January 2026*  
*Next review: After v0.2 completion*
