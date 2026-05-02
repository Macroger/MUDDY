using Server.Core.CommandPipeline.Parser;
using Server.Core.CommandPipeline.Types;
using Shared.Identity;
using Shared.Protocol.Transport;
using Shared.Protocol.Types;
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

    private TransportEnvelope BuildEnvelope(string json)
    {
        return new TransportEnvelope(
            messageId: new MessageId(1),
            messageType: TransportMessageType.Command,
            flags: MessageFlags.None,
            payload: Encoding.UTF8.GetBytes(json),
            connectionId: _connectionId,
            sessionId: SessionId.Unauthenticated
        );
    }

    // -------------------------------------------------------------------------
    // Parse — success
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
    // Parse — missing verb
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
    // Parse — invalid JSON
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
