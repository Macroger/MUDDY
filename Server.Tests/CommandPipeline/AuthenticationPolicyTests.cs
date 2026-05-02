using Moq;
using Server.Core.CommandPipeline.Policies;
using Server.Core.Domain.Authentication;
using Shared.Identity;
using Shared.Protocol.Transport;
using Shared.Protocol.Types;

namespace Server.Tests.CommandPipeline;

/// <summary>
/// Unit tests for AuthenticationPolicy.
/// Covers unauthenticated pass-through and rejection of invalid sessions.
/// </summary>
[TestClass]
public class AuthenticationPolicyTests
{
    private Mock<IAuthenticationService> _mockAuthService = null!;
    private AuthenticationPolicy _policy = null!;
    private ConnectionId _connectionId;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _policy = new AuthenticationPolicy(_mockAuthService.Object);
        _connectionId = new ConnectionId(Guid.NewGuid().ToString());
    }

    private TransportEnvelope BuildEnvelope(SessionId? sessionId)
    {
        return new TransportEnvelope(
            messageId: new MessageId(1),
            messageType: TransportMessageType.Command,
            flags: MessageFlags.None,
            payload: Array.Empty<byte>(),
            connectionId: _connectionId,
            sessionId: sessionId
        );
    }

    // -------------------------------------------------------------------------
    // Unauthenticated session — always allowed
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CheckPolicy_UnauthenticatedSession_ReturnsSuccess()
    {
        var envelope = BuildEnvelope(SessionId.Unauthenticated);

        var result = await _policy.CheckPolicyAsync(envelope);

        Assert.IsTrue(result.IsValid);
    }

    // -------------------------------------------------------------------------
    // Authenticated but invalid session — rejected
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CheckPolicy_InvalidSession_ReturnsFailure()
    {
        var sessionId = new SessionId(Guid.NewGuid());
        _mockAuthService
            .Setup(s => s.ValidateSessionAsync(sessionId, _connectionId))
            .ReturnsAsync(false);

        var envelope = BuildEnvelope(sessionId);

        var result = await _policy.CheckPolicyAsync(envelope);

        Assert.IsFalse(result.IsValid);
        Assert.IsNotNull(result.ErrorMessage);
    }
}
