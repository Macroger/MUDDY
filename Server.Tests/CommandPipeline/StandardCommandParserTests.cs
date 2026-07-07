// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.Parser;
using Server.Core.CommandPipeline.Types;
using Shared.Identity;
using Shared.Network.Transport;
using Shared.Network.Types;
using System.Text;

namespace Server.Tests.CommandPipeline;

/// <summary>
/// Unit tests for StandardCommandParser.
/// Covers happy-path parsing, missing verb, and invalid JSON.
/// </summary>
[TestClass]
public class StandardCommandParserTests
{
    private StandardCommandParser _parser = null!;
    private ConnectionId _connectionId;

    [TestInitialize]
    public void TestInitialize()
    {
        _parser = new StandardCommandParser();
        _connectionId = new ConnectionId(Guid.NewGuid().ToString());
    }

    private MessageEnvelope BuildEnvelope(string json)
    {
        return new MessageEnvelope(
            messageId: new MessageId(1),
            messageType: PacketType.Command,
            flags: MessageFlags.None,
            payload: Encoding.UTF8.GetBytes(json),
            connectionId: _connectionId,
            sessionId: SessionId.Unauthenticated
        );
    }

    // -------------------------------------------------------------------------
    // Parse â€” success
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Parse_ValidJsonWithVerbAndArgs_ReturnsSuccessWithCorrectFields()
    {
        var envelope = BuildEnvelope("{\"verb\":\"move\",\"args\":[\"north\"]}");

        var result = _parser.Parse(envelope);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Command);
        Assert.AreEqual("move", result.Command.CommandType);
        Assert.AreEqual("north", result.Command.Arguments[0]);
    }

    // -------------------------------------------------------------------------
    // Parse â€” missing verb
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Parse_MissingVerb_ReturnsFailureWithMissingVerbError()
    {
        var envelope = BuildEnvelope("{\"args\":[\"north\"]}");

        var result = _parser.Parse(envelope);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(CommandErrorType.MISSING_VERB, result.ErrorType);
    }

    // -------------------------------------------------------------------------
    // Parse â€” invalid JSON
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Parse_InvalidJson_ReturnsFailure()
    {
        var envelope = BuildEnvelope("not-valid-json{{");

        var result = _parser.Parse(envelope);

        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Command);
    }
}
