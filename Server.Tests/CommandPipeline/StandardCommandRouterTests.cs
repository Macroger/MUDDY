// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Moq;
using Server.Core.CommandPipeline.CommandHandler;
using Server.Core.CommandPipeline.CommandRouter;
using Server.Core.CommandPipeline.Types;
using Shared.Identity;

namespace Server.Tests.CommandPipeline;

/// <summary>
/// Unit tests for StandardCommandRouter.
/// Covers handler registration, routing to the correct handler, and null-returns for unknown verbs.
/// </summary>
[TestClass]
public class StandardCommandRouterTests
{
    private StandardCommandRouter _router = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _router = new StandardCommandRouter();
    }

    private static ParsedCommand BuildCommand(string verb)
        => new ParsedCommand { CommandType = verb, Arguments = Array.Empty<string>(), MsgId = new MessageId(1) };

    // -------------------------------------------------------------------------
    // Route â€” registered verb
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Route_RegisteredVerb_ReturnsCorrectHandler()
    {
        var mockHandler = new Mock<ICommandHandler>();
        _router.RegisterHandler("move", mockHandler.Object);

        var result = _router.Route(BuildCommand("move"));

        Assert.AreEqual(mockHandler.Object, result);
    }

    // -------------------------------------------------------------------------
    // Route â€” unknown verb
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Route_UnregisteredVerb_ReturnsNull()
    {
        var result = _router.Route(BuildCommand("unknown_command"));

        Assert.IsNull(result);
    }

    // -------------------------------------------------------------------------
    // RegisterHandler â€” guard clauses
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RegisterHandler_NullVerb_ThrowsArgumentNullException()
    {
        var mockHandler = new Mock<ICommandHandler>();

        Assert.ThrowsExactly<ArgumentNullException>(
            () => _router.RegisterHandler(null!, mockHandler.Object));
    }

    [TestMethod]
    public void RegisterHandler_DuplicateVerb_ReturnsFalse()
    {
        var mockHandler = new Mock<ICommandHandler>();
        _router.RegisterHandler("say", mockHandler.Object);

        bool result = _router.RegisterHandler("say", new Mock<ICommandHandler>().Object);

        Assert.IsFalse(result);
    }
}
