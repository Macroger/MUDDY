// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Moq;
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.World;
using Server.Core.Persistence;
using Shared.Domain.Player;
using Shared.Identity;

namespace Server.Tests.CommandPipeline;

/// <summary>
/// Unit tests for StandardCommandContextBuilder.
/// Covers the no-player-found error path and the successful context construction path.
/// </summary>
[TestClass]
public class StandardCommandContextBuilderTests
{
    private Mock<IPlayerRepository> _mockPlayerRepo = null!;
    private Mock<IWorldRepository> _mockWorldRepo = null!;
    private StandardCommandContextBuilder _builder = null!;
    private ConnectionId _connectionId;
    private ParsedCommand _command = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockPlayerRepo = new Mock<IPlayerRepository>();
        _mockWorldRepo = new Mock<IWorldRepository>();
        _builder = new StandardCommandContextBuilder(_mockPlayerRepo.Object, _mockWorldRepo.Object);
        _connectionId = new ConnectionId(Guid.NewGuid().ToString());
        _command = new ParsedCommand
        {
            CommandType = "look",
            Arguments = Array.Empty<string>(),
            MsgId = new MessageId(1)
        };
    }

    // -------------------------------------------------------------------------
    // No player found â€” error context
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task BuildContext_NoPlayerFound_ReturnsErrorContext()
    {
        _mockPlayerRepo
            .Setup(r => r.GetPlayerByConnectionIdAsync(_connectionId))
            .ReturnsAsync((PlayerState?)null);

        var context = await _builder.BuildContextAsync(_connectionId, _command);

        Assert.IsFalse(context.Success);
        Assert.IsNull(context.PlayerState);
        Assert.IsNotNull(context.ErrorMessage);
    }

    // -------------------------------------------------------------------------
    // Player found â€” success context
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task BuildContext_PlayerAndRoomFound_ReturnsSuccessContext()
    {
        var roomId = new RoomId("tavern");
        var player = new PlayerState
        {
            ConnId = _connectionId,
            PlayerName = "Alice",
            CurrentLocation = roomId,
            ActiveConditions = new HashSet<PlayerCondition>()
        };
        var room = new RoomState(roomId, "A tavern.", new HashSet<RoomCondition>(), new HashSet<ConnectionId>());
        var world = new WorldState(
            new Dictionary<RoomId, RoomState> { { roomId, room } },
            new HashSet<ActiveWorldConditions>());

        _mockPlayerRepo.Setup(r => r.GetPlayerByConnectionIdAsync(_connectionId)).ReturnsAsync(player);
        _mockWorldRepo.Setup(r => r.GetRoomAsync(roomId)).ReturnsAsync(room);
        _mockWorldRepo.Setup(r => r.GetWorldStateAsync()).ReturnsAsync(world);

        var context = await _builder.BuildContextAsync(_connectionId, _command);

        Assert.IsTrue(context.Success);
        Assert.IsNotNull(context.PlayerState);
        Assert.IsNotNull(context.WorldState);
    }
}
