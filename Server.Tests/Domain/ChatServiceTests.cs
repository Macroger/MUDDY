using Moq;
using Server.Core.CommandPipeline.ContextBuilder;
using Shared.Domain.Player;
using Server.Core.Domain.Services.ConcreteClasses;
using Server.Core.Domain.World;
using Server.Core.Infrastructure.Identity.MessageId;
using Shared.EventBus;
using Shared.Identity;

namespace Server.Tests.Domain;

/// <summary>
/// Unit tests for ChatService.
/// Covers rejection of empty messages and successful broadcast with event publication.
/// </summary>
[TestClass]
public class ChatServiceTests
{
    private Mock<IEventBus> _mockEventBus;
    private Mock<IMessageIdGenerator> _mockMessageIdGenerator;
    private ChatService _service;

    private ConnectionId _connectionId;
    private RoomId _roomId;
    private PlayerState _player;
    private WorldState _world;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockEventBus = new Mock<IEventBus>();
        _mockMessageIdGenerator = new Mock<IMessageIdGenerator>();
        _mockMessageIdGenerator.Setup(g => g.New()).Returns(new MessageId(1));

        _service = new ChatService(_mockEventBus.Object, _mockMessageIdGenerator.Object);

        _connectionId = new ConnectionId(Guid.NewGuid().ToString());
        _roomId = new RoomId("tavern");

        _player = new PlayerState
        {
            ConnId = _connectionId,
            PlayerName = "Alice",
            CurrentLocation = _roomId,
            ActiveConditions = new HashSet<PlayerCondition>()
        };

        var room = new RoomState(
            _roomId,
            "A cozy tavern.",
            new HashSet<RoomCondition>(),
            new HashSet<ConnectionId> { _connectionId });

        _world = new WorldState(
            new Dictionary<RoomId, RoomState> { { _roomId, room } },
            new HashSet<ActiveWorldConditions>());
    }

    // -------------------------------------------------------------------------
    // BroadcastMessageAsync — empty message
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task BroadcastMessage_EmptyMessage_ReturnsFailure()
    {
        var result = await _service.BroadcastMessageAsync(_player, _world, "");

        Assert.IsFalse(result.Success);
    }

    // -------------------------------------------------------------------------
    // BroadcastMessageAsync — valid message
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task BroadcastMessage_ValidMessage_ReturnsSuccessAndPublishesEvent()
    {
        var result = await _service.BroadcastMessageAsync(_player, _world, "Hello!");

        Assert.IsTrue(result.Success);
        _mockEventBus.Verify(
            e => e.Publish(It.IsAny<EventMessageType>(), It.IsAny<EventEnvelope>()),
            Times.Once);
    }
}
