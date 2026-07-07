// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Moq;
using Server.Core.CommandPipeline.Policies;
using Server.Core.Domain.Authentication;
using Shared.Identity;
using Shared.Network.Transport;
using Shared.Network.Types;

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

    private MessageEnvelope BuildEnvelope(SessionId? sessionId)
    {
        return new MessageEnvelope(
            messageId: new MessageId(1),
            messageType: PacketType.Command,
            flags: MessageFlags.None,
            payload: Array.Empty<byte>(),
            connectionId: _connectionId,
            sessionId: sessionId
        );
    }

    // -------------------------------------------------------------------------
    // Unauthenticated session â€” always allowed
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CheckPolicy_UnauthenticatedSession_ReturnsSuccess()
    {
        var envelope = BuildEnvelope(SessionId.Unauthenticated);

        var result = await _policy.CheckPolicyAsync(envelope);

        Assert.IsTrue(result.IsValid);
    }

    // -------------------------------------------------------------------------
    // Authenticated but invalid session â€” rejected
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
