// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.Domain.Services.WorldQueryService;
using Server.Core.Domain.World;
using Shared.Domain.Player;
using Shared.Identity;

namespace Server.Tests.Domain;

/// <summary>
/// Unit tests for WorldQueryService.
/// Covers the look command returning the current room's description.
/// </summary>
[TestClass]
public class WorldQueryServiceTests
{
    private WorldQueryService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _service = new WorldQueryService();
    }

    // -------------------------------------------------------------------------
    // LookAtRoom â€” known room
    // -------------------------------------------------------------------------

    [TestMethod]
    public void LookAtRoom_KnownRoom_ReturnsRoomDescription()
    {
        var roomId = new RoomId("tavern");
        const string description = "A cozy tavern.";
        var room = new RoomState(roomId, description, new HashSet<RoomCondition>(), new HashSet<ConnectionId>());
        var world = new WorldState(
            new Dictionary<RoomId, RoomState> { { roomId, room } },
            new HashSet<ActiveWorldConditions>());
        var player = new PlayerState
        {
            ConnId = new ConnectionId(Guid.NewGuid().ToString()),
            PlayerName = "Alice",
            CurrentLocation = roomId,
            ActiveConditions = new HashSet<PlayerCondition>()
        };

        var result = _service.LookAtRoom(player, world);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(description, result.Message);
    }
}
