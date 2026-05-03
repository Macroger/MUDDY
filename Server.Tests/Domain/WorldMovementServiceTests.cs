// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Moq;
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.Domain.Services.WorldMovementService;
using Server.Core.Domain.World;
using Server.Core.Persistence;
using Shared.Domain.Player;
using Shared.Identity;

namespace Server.Tests.Domain;

/// <summary>
/// Unit tests for WorldMovementService.
/// Covers a successful move to a valid exit and a failed move in an unknown direction.
/// </summary>
[TestClass]
public class WorldMovementServiceTests
{
    private Mock<IPlayerRepository> _mockPlayerRepo = null!;
    private Mock<IWorldRepository> _mockWorldRepo = null!;
    private WorldMovementService _service = null!;

    private ConnectionId _connectionId;
    private RoomId _tavernId;
    private RoomId _townId;
    private PlayerState _player = null!;
    private RoomState _tavernRoom = null!;
    private RoomState _townRoom = null!;
    private WorldState _world = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockPlayerRepo = new Mock<IPlayerRepository>();
        _mockWorldRepo = new Mock<IWorldRepository>();
        _service = new WorldMovementService(_mockPlayerRepo.Object, _mockWorldRepo.Object);

        _connectionId = new ConnectionId(Guid.NewGuid().ToString());
        _tavernId = new RoomId("tavern");
        _townId = new RoomId("town");

        _player = new PlayerState
        {
            ConnId = _connectionId,
            PlayerName = "Alice",
            CurrentLocation = _tavernId,
            ActiveConditions = new HashSet<PlayerCondition>()
        };

        _tavernRoom = new RoomState(
            _tavernId,
            "A cozy tavern.",
            new HashSet<RoomCondition>(),
            new HashSet<ConnectionId>(),
            new Dictionary<string, RoomId> { { "north", _townId } });

        _townRoom = new RoomState(
            _townId,
            "A busy town.",
            new HashSet<RoomCondition>(),
            new HashSet<ConnectionId>());

        _world = new WorldState(
            new Dictionary<RoomId, RoomState> { { _tavernId, _tavernRoom }, { _townId, _townRoom } },
            new HashSet<ActiveWorldConditions>());

        _mockPlayerRepo.Setup(r => r.GetPlayerByConnectionIdAsync(_connectionId)).ReturnsAsync(_player);
        _mockWorldRepo.Setup(r => r.GetRoomAsync(_tavernId)).ReturnsAsync(_tavernRoom);
        _mockWorldRepo.Setup(r => r.GetRoomAsync(_townId)).ReturnsAsync(_townRoom);
        _mockWorldRepo.Setup(r => r.UpdateRoomAsync(It.IsAny<RoomState>())).Returns(Task.CompletedTask);
        _mockPlayerRepo.Setup(r => r.UpsertPlayerAsync(It.IsAny<PlayerState>())).Returns(Task.CompletedTask);
    }

    // -------------------------------------------------------------------------
    // MovePlayerAsync â€” valid direction
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task MovePlayer_ValidDirection_ReturnsSuccess()
    {
        var result = await _service.MovePlayerAsync(_player, _world, "north");

        Assert.IsTrue(result.Success);
        StringAssert.Contains(result.Message, "north");
    }

    // -------------------------------------------------------------------------
    // MovePlayerAsync â€” invalid direction
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task MovePlayer_InvalidDirection_ReturnsFailure()
    {
        var result = await _service.MovePlayerAsync(_player, _world, "south");

        Assert.IsFalse(result.Success);
    }
}
