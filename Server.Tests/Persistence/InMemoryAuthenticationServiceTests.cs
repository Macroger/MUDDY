using Moq;
using Server.Core.Infrastructure.Identity.SessionId;
using Server.Core.Persistence;
using Shared.EventBus;
using Shared.Identity;

namespace Server.Tests.Persistence;

/// <summary>
/// Unit tests for InMemoryAuthenticationService.
/// Covers session creation, validation, and removal.
///
/// IEventBus and ISessionIdGenerator are mocked using Moq so we can
/// test authentication logic in isolation without a real event bus or ID generator.
/// </summary>
[TestClass]
public class InMemoryAuthenticationServiceTests
{
    private InMemoryAuthenticationService _service;
    private Mock<IEventBus> _mockEventBus;
    private Mock<ISessionIdGenerator> _mockSessionIdGenerator;
    private ConnectionId _connectionId;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockEventBus = new Mock<IEventBus>();
        _mockSessionIdGenerator = new Mock<ISessionIdGenerator>();

        // Every call to New() returns a fresh unique session ID
        _mockSessionIdGenerator
            .Setup(g => g.New())
            .Returns(() => new SessionId(Guid.NewGuid()));

        _connectionId = new ConnectionId(Guid.NewGuid().ToString());

        _service = new InMemoryAuthenticationService(
            _mockEventBus.Object,
            _mockSessionIdGenerator.Object
        );
    }

    // -------------------------------------------------------------------------
    // CreateSessionAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CreateSession_ReturnsNonUnauthenticatedSessionId()
    {
        var sessionId = await _service.CreateSessionAsync(_connectionId, "Alice");

        // The returned session ID should never be the "unauthenticated" placeholder (Guid.Empty)
        Assert.AreNotEqual(SessionId.Unauthenticated, sessionId);
    }

    [TestMethod]
    public async Task CreateSession_PublishesEventOnTheBus()
    {
        await _service.CreateSessionAsync(_connectionId, "Alice");

        // The event bus must be called at least once to log the new session
        _mockEventBus.Verify(
            e => e.Publish(It.IsAny<EventMessageType>(), It.IsAny<EventEnvelope>()),
            Times.AtLeastOnce
        );
    }

    // -------------------------------------------------------------------------
    // ValidateSessionAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ValidateSession_ReturnsTrue_ForValidSessionAndMatchingConnection()
    {
        var sessionId = await _service.CreateSessionAsync(_connectionId, "Alice");

        bool isValid = await _service.ValidateSessionAsync(sessionId, _connectionId);

        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public async Task ValidateSession_ReturnsFalse_WhenSessionIsNull()
    {
        bool isValid = await _service.ValidateSessionAsync(null, _connectionId);

        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public async Task ValidateSession_ReturnsFalse_WhenSessionIsUnauthenticated()
    {
        // SessionId.Unauthenticated (Guid.Empty) is always treated as invalid
        bool isValid = await _service.ValidateSessionAsync(SessionId.Unauthenticated, _connectionId);

        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public async Task ValidateSession_ReturnsFalse_WhenConnectionIdDoesNotMatch()
    {
        // Create a session tied to _connectionId, then validate with a completely different connection
        var sessionId = await _service.CreateSessionAsync(_connectionId, "Alice");

        var differentConnection = new ConnectionId(Guid.NewGuid().ToString());
        bool isValid = await _service.ValidateSessionAsync(sessionId, differentConnection);

        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public async Task ValidateSession_ReturnsFalse_ForUnknownSessionId()
    {
        // A session ID that was never created by this service should not be valid
        var unknownSession = new SessionId(Guid.NewGuid());

        bool isValid = await _service.ValidateSessionAsync(unknownSession, _connectionId);

        Assert.IsFalse(isValid);
    }

    // -------------------------------------------------------------------------
    // RemoveSession
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task RemoveSession_ThenValidate_ReturnsFalse()
    {
        // Arrange — create a valid session
        var sessionId = await _service.CreateSessionAsync(_connectionId, "Alice");

        // Act — remove it
        await _service.RemoveSession(sessionId);

        // Assert — the removed session should no longer validate
        bool isValid = await _service.ValidateSessionAsync(sessionId, _connectionId);
        Assert.IsFalse(isValid);
    }
}
