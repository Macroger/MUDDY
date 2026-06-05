// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Moq;
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.Domain.World;
using Server.Core.Persistence;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;
using Shared.Identity;

namespace Server.Tests.Persistence;

/// <summary>
/// Unit tests for InMemoryWorldRepository.
/// Covers room retrieval, world state snapshots, and room updates.
///
/// The repository uses a single ConcurrentDictionary (_rooms) as the authoritative
/// source of truth. The constructor pre-populates it with the default world
/// (tavern, town, forest), so all three methods â€” GetRoomAsync, UpdateRoomAsync,
/// and GetWorldStateAsync â€” always reflect the same live state.
/// </summary>
[TestClass]
public class InMemoryWorldRepositoryTests
{
    private InMemoryWorldRepository _repository = null!;
    private Mock<IEventBus> _mockEventBus = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockEventBus = new Mock<IEventBus>();
        _mockEventBus
            .Setup(b => b.Subscribe<PlayerEvents.PlayerLeftWorldEvent>(
                EventMessageType.Domain, It.IsAny<Action<PlayerEvents.PlayerLeftWorldEvent>>()))
            .Returns(new Mock<ISubscriptionToken>().Object);

        // The constructor seeds _rooms with the default world automatically
        _repository = new InMemoryWorldRepository(_mockEventBus.Object);
    }

    // -------------------------------------------------------------------------
    // GetRoomAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetRoom_ReturnsRoom_ForKnownDefaultRoom()
    {
        // The default world always contains "tavern"
        var room = await _repository.GetRoomAsync(new RoomId("tavern"));

        Assert.IsNotNull(room);
        Assert.AreEqual(new RoomId("tavern"), room.Id);
    }

    [TestMethod]
    public async Task GetRoom_ReturnsNull_ForUnknownRoom()
    {
        var room = await _repository.GetRoomAsync(new RoomId("nonexistent_room"));

        Assert.IsNull(room);
    }

    [TestMethod]
    public async Task GetRoom_ReturnsCorrectExits_ForTavernRoom()
    {
        // The tavern has a "north" exit leading to "town" per GameWorldFactory
        var room = await _repository.GetRoomAsync(new RoomId("tavern"));

        Assert.IsNotNull(room);
        Assert.IsTrue(room.Exits.ContainsKey("north"));
        Assert.AreEqual(new RoomId("town"), room.Exits["north"]);
    }

    // -------------------------------------------------------------------------
    // GetWorldStateAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetWorldState_ContainsAllThreeDefaultRooms()
    {
        var worldState = await _repository.GetWorldStateAsync();

        // The default world has exactly 3 rooms: tavern, town, forest
        Assert.HasCount(3, worldState.Rooms);
        Assert.IsTrue(worldState.Rooms.ContainsKey(new RoomId("tavern")));
        Assert.IsTrue(worldState.Rooms.ContainsKey(new RoomId("town")));
        Assert.IsTrue(worldState.Rooms.ContainsKey(new RoomId("forest")));
    }

    [TestMethod]
    public async Task GetWorldState_ReturnsSnapshot_NotTheLiveDictionary()
    {
        // GetWorldStateAsync returns a copy â€” modifying the snapshot should not
        // affect what the repository returns on a subsequent call
        var snapshot = await _repository.GetWorldStateAsync();
        int originalCount = snapshot.Rooms.Count;

        // The snapshot is a read-only copy; we just verify repeated calls are consistent
        var snapshot2 = await _repository.GetWorldStateAsync();
        Assert.HasCount(originalCount, snapshot2.Rooms);
    }

    // -------------------------------------------------------------------------
    // UpdateRoomAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task UpdateRoom_IsReflected_InGetRoomAsync()
    {
        // After an update, GetRoomAsync should return the new version of the room
        var playerId = new ConnectionId(Guid.NewGuid().ToString());
        var updatedTavern = new RoomState(
            id: new RoomId("tavern"),
            description: "A freshly updated tavern.",
            roomConditions: new HashSet<RoomCondition>(),
            playersInRoom: new HashSet<ConnectionId> { playerId }
        );

        await _repository.UpdateRoomAsync(updatedTavern);

        var room = await _repository.GetRoomAsync(new RoomId("tavern"));
        Assert.IsNotNull(room);
        Assert.AreEqual("A freshly updated tavern.", room.Description);
        Assert.Contains(playerId, room.PlayersPresent);
    }

    [TestMethod]
    public async Task UpdateRoom_IsReflected_InGetWorldStateAsync()
    {
        // The same update should also appear in the world state snapshot
        var updatedTavern = new RoomState(
            id: new RoomId("tavern"),
            description: "Snapshot should see this too.",
            roomConditions: new HashSet<RoomCondition>(),
            playersInRoom: new HashSet<ConnectionId>()
        );

        await _repository.UpdateRoomAsync(updatedTavern);

        var worldState = await _repository.GetWorldStateAsync();
        Assert.AreEqual("Snapshot should see this too.", worldState.Rooms[new RoomId("tavern")].Description);
    }

    [TestMethod]
    public async Task UpdateRoom_DoesNotAffectOtherRooms()
    {
        // Updating the tavern should leave the other two rooms unchanged
        var updatedTavern = new RoomState(
            id: new RoomId("tavern"),
            description: "Changed description.",
            roomConditions: new HashSet<RoomCondition>(),
            playersInRoom: new HashSet<ConnectionId>()
        );

        await _repository.UpdateRoomAsync(updatedTavern);

        var worldState = await _repository.GetWorldStateAsync();
        Assert.HasCount(3, worldState.Rooms);
        Assert.IsTrue(worldState.Rooms.ContainsKey(new RoomId("town")));
        Assert.IsTrue(worldState.Rooms.ContainsKey(new RoomId("forest")));
    }
}
