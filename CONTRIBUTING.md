# Contributing to MUDDY

Thank you for your interest in contributing to MUDDY! This is a learning-focused MUD template project designed to help developers understand game architecture, .NET patterns, and command-driven systems.

---

## Welcome, Contributors!

MUDDY is an **educational project** and I am excited to have contributors that can help improve:

- Code quality and clarity
- Documentation and examples
- Bug fixes and feature additions
- Test coverage
- Architectural improvements

---

## Getting Started

### Prerequisites

- .NET 10 SDK or later
- Visual Studio 2026+ or compatible IDE
- Git

### Setting Up Your Development Environment

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/MUDDY.git
   cd MUDDY
   ```
3. **Add upstream remote** (to stay synced):
   ```bash
   git remote add upstream https://github.com/Macroger/MUDDY.git
   ```
4. **Build the project**:
   ```bash
   dotnet build
   ```
5. **Run tests** (if applicable):
   ```bash
   dotnet test
   ```

### Understanding the Fork Workflow

This project uses the **fork workflow**, which is the standard for open-source collaboration on GitHub. If this is your first time using it, don't worry — we'll break it down.

#### What is a Fork?

A **fork** is your own copy of the entire MUDDY project on GitHub. When you fork MUDDY:
- You get a complete copy: `github.com/YOUR-USERNAME/MUDDY`
- You can make changes without affecting the original project
- You can push code directly to your fork (since you own it)
- The original project remains untouched until you submit a pull request

#### How Origin and Upstream Work

When you clone your fork with the commands above, you're setting up two connections:

1. **`origin`** — Points to *your fork* (`YOUR-USERNAME/MUDDY`)
   - You can push to this freely
   - This is where you upload your feature branches
   - Think of it as "my copy on GitHub"

2. **`upstream`** — Points to *the main project* (`Macroger/MUDDY`)
   - You have read access (can pull updates)
   - You cannot push directly to this (safety feature)
   - Think of it as "the official project"

#### The Workflow in Action

Here's how a typical contribution flows:

```
1. Pull latest from upstream (stay current with other work)
   git fetch upstream

2. Create a feature branch and make your changes
   git checkout -b feature/my-change
   [make code changes]
   git commit -m "feat: describe what I did"

3. Push to your fork (origin)
   git push origin feature/my-change

4. Open a pull request on GitHub
   GitHub lets you say: "Hey Macroger, can you review my changes?"

5. Maintainer reviews and merges
   If approved, your code joins the main project!
```

#### Why This Matters

- **Safety** — You can't break the main project because you don't have write access to it
- **Organization** — Pull requests create a formal review process and discussion space
- **Collaboration** — Multiple people can work on different features simultaneously without conflicts
- **Learning** — You see exactly what professional open-source looks like

This is how millions of developers collaborate on GitHub projects every day.

---

## Before You Start

### Read the Documentation

- **[CODING_STYLE.md](./CODING_STYLE.md)** — Code style and conventions
- **[LEARNING.md](./LEARNING.md)** (if available) — Architecture overview
- **Existing code** — Look at similar implementations for patterns

### Check Existing Issues (Recommended)

Browse [open issues](https://github.com/Macroger/MUDDY/issues) to see if someone is already working on your idea or if there's guidance on what's needed. **It's also helpful to comment on an issue before you start** — this way:

- You avoid duplicating work someone else is already doing
- Maintainers can give feedback on your approach upfront (saves you time later)
- You can discuss design decisions before investing effort
- Others know you're planning to work on it

Of course, if you want to just jump in and open a pull request, that's fine too! We'll discuss everything in the review.

---

## Making Changes

### Create a Feature Branch

Never commit to `main`. Use a descriptive branch name:

```bash
git checkout -b feature/add-emote-command
git checkout -b fix/movement-validation
git checkout -b docs/update-readme
```

Branch naming convention: `{type}/{short-description}`

Valid types:
- `feature/` — New functionality
- `fix/` — Bug fixes
- `docs/` — Documentation updates
- `refactor/` — Code improvements (no behavior change)
- `test/` — Test additions or fixes

### Code Guidelines

Follow the conventions in [CODING_STYLE.md](./CODING_STYLE.md):

- Use **sealed classes** for concrete implementations
- Write **XML documentation** for public APIs
- Validate inputs and **return early** for error cases
- Use **null pattern matching** (`is null`) over `== null`
- Keep **private fields `readonly`** where possible
- Add **inline comments** only for non-obvious logic

### Commit Messages

Write clear, descriptive commit messages:

```
feat: add emote command for player expression

- Implement new EmoteCommandHandler
- Register emote command in SystemInitializer
- Add unit tests for emote validation
- Update documentation with emote syntax

Fixes #42
```

Format:
- Start with type: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`
- Use imperative mood ("add" not "added")
- Keep first line under 50 characters
- Reference issues if applicable: `Fixes #123`, `Related to #456`

---

## Testing

### Running Tests

```bash
dotnet test
```

### Writing Tests

- Use test names that describe behavior: `Handler_Method_ExpectedOutcome`
- Example: `ChatCommandHandler_ExecuteAsync_ReturnsSuccessWhenValidMessageGiven`
- One assertion per test (when possible)
- Include comments explaining complex test setup

### Test Coverage

Aim for reasonable coverage, especially for:
- Command handlers
- Validation logic
- Domain services

---

## Submitting a Pull Request

### Before You Submit

1. **Sync with upstream**:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```
2. **Build and test locally**:
   ```bash
   dotnet build
   dotnet test
   ```
3. **Review your own code** — catch obvious issues before submission

### Creating the PR

1. **Push your branch** to your fork:
   ```bash
   git push origin feature/your-feature
   ```
2. **Open a Pull Request** on GitHub with:
   - **Clear title** — summarize your change
   - **Detailed description** — explain what, why, and how
   - **Link to issues** — reference related issues (e.g., "Closes #42")
   - **Screenshots** (if UI-related) — show before/after if applicable

### PR Description Template

```markdown
## Description
Brief explanation of what this PR does and why.

## Related Issues
Closes #123
Related to #456

## Changes
- Change 1
- Change 2
- Change 3

## Testing
How did you test these changes? (manual, unit tests, etc.)

## Checklist
- [ ] Code follows CODING_STYLE.md guidelines
- [ ] XML documentation added for public APIs
- [ ] Tests added/updated for new functionality
- [ ] Commit messages follow convention
- [ ] No breaking changes (or documented if unavoidable)
```

---

## Code Review

### What to Expect

- **Constructive feedback** — we'll suggest improvements
- **Questions** — maintainers may ask about your approach
- **Iteration** — you may need to make updates
- **Respect** — we value your contribution!

### Addressing Feedback

1. Make requested changes on your branch
2. Push the updates (no force push)
3. Respond to review comments
4. Request another review

---

## Contributor License Agreement

By contributing to MUDDY, you agree that your contributions are licensed under the **Apache License 2.0**, consistent with the project license. You retain ownership of your work, and the license simply allows the project to use and redistribute it.

---

## Community Guidelines

### Be Respectful

- Treat everyone with respect
- No harassment, discrimination, or abuse
- Assume good intent in discussions

### Ask Questions

- This is a learning project — questions are encouraged!
- If something is unclear, ask in an issue
- Help others when you can

### Reporting Issues

Found a bug? Have a suggestion? [Open an issue](https://github.com/Macroger/MUDDY/issues) with:

- **Clear title** — summarize the problem
- **Detailed description** — what happened, what you expected
- **Steps to reproduce** — if it's a bug
- **Environment** — .NET version, OS, etc.
- **Screenshots/logs** — if applicable

---

## Development Tips

### Project Structure

- **Server.Core/** — Main game logic, handlers, services
- **Shared/** — Shared types, networking, events
- **Server.Tests/** — Unit tests
- **Client.Core/** — Client-side command pipeline

### Key Entry Points

- **SystemInitializer.cs** — Application startup, handler registration
- **StandardCommandRouter.cs** — Command dispatch logic
- **ICommandHandler interface** — Implement this for new command categories

### Local Testing

```bash
# Build only
dotnet build

# Build and run tests
dotnet build && dotnet test

# Rebuild clean
dotnet clean && dotnet build
```

---

## Questions?

- **Documentation** — Check the README and style guides first
- **Issues** — Search existing issues before creating new ones
- **Discussions** — Start a discussion if you have questions (GitHub Discussions)

---

## License

By contributing, you agree your contributions are licensed under the Apache License 2.0, the same license as the MUDDY project.

---

**Thank you for contributing to MUDDY!** Your work helps make this a better learning resource for the community.

---

## About This Document

This contributing guide was created with the assistance of Claude (Anthropic's AI assistant) under the guidance of Matthew Schatz (Macroger). The document was designed to be welcoming and educational while maintaining professional open-source standards. All processes and guidelines reflect the project maintainers' vision for how MUDDY should grow as a community learning resource.
