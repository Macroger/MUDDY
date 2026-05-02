// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.Domain.Services.PlayerQueryService;
using Server.Core.Domain.World;
using Shared.Domain.Player;
using Shared.Identity;

namespace Server.Tests.Domain;

/// <summary>
/// Unit tests for PlayerQueryService.
/// Covers player status formatting and the list-players-in-room paths.
/// </summary>
[TestClass]
public class PlayerQueryServiceTests
{
    private PlayerQueryService _service = null!;
    private ConnectionId _connectionId;
    private RoomId _roomId;

    [TestInitialize]
    public void TestInitialize()
    {
        _service = new PlayerQueryService();
        _connectionId = new ConnectionId(Guid.NewGuid().ToString());
        _roomId = new RoomId("tavern");
    }

    // -------------------------------------------------------------------------
    // GetPlayerStatus
    // -------------------------------------------------------------------------

    [TestMethod]
    public void GetPlayerStatus_ReturnsSuccessWithPlayerNameInMessage()
    {
        var player = new PlayerState
        {
            ConnId = _connectionId,
            PlayerName = "Alice",
            CurrentLocation = _roomId,
            ActiveConditions = new HashSet<PlayerCondition>()
        };
        var room = new RoomState(_roomId, "A tavern.", new HashSet<RoomCondition>(), new HashSet<ConnectionId>());
        var world = new WorldState(
            new Dictionary<RoomId, RoomState> { { _roomId, room } },
            new HashSet<ActiveWorldConditions>());

        var result = _service.GetPlayerStatus(player, world);

        Assert.IsTrue(result.Success);
        StringAssert.Contains(result.Message, "Alice");
    }

    // -------------------------------------------------------------------------
    // ListPlayersInRoom â€” empty room
    // -------------------------------------------------------------------------

    [TestMethod]
    public void ListPlayersInRoom_NoPlayersInRoom_ReturnsNoPlayersMessage()
    {
        var player = new PlayerState
        {
            ConnId = _connectionId,
            PlayerName = "Alice",
            CurrentLocation = _roomId,
            ActiveConditions = new HashSet<PlayerCondition>()
        };
        var room = new RoomState(_roomId, "A tavern.", new HashSet<RoomCondition>(), new HashSet<ConnectionId>());
        var world = new WorldState(
            new Dictionary<RoomId, RoomState> { { _roomId, room } },
            new HashSet<ActiveWorldConditions>());

        var result = _service.ListPlayersInRoom(player, world);

        Assert.IsTrue(result.Success);
        StringAssert.Contains(result.Message, "no other players");
    }
}
