// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Moq;
using Server.Core.Persistence;
using Shared.Domain.Player;
using Shared.EventBus;
using Shared.EventBus.DomainEvents;
using Shared.EventBus.SubscriptionToken;
using Shared.Identity;

namespace Server.Tests.Persistence;

/// <summary>
/// Unit tests for InMemoryPlayerRepository.
/// Covers adding, retrieving, updating, and removing player state.
/// </summary>
[TestClass]
public class InMemoryPlayerRepositoryTests
{
    private InMemoryPlayerRepository _repository = null!;
    private Mock<IEventBus> _mockEventBus = null!;
    private ConnectionId _connectionId;
    private PlayerState _testPlayer = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockEventBus = new Mock<IEventBus>();
        _mockEventBus
            .Setup(b => b.Subscribe<NetworkEvents.ClientDisconnectedEvent>(
                EventMessageType.Network, It.IsAny<Action<NetworkEvents.ClientDisconnectedEvent>>()))
            .Returns(new Mock<ISubscriptionToken>().Object);

        _repository = new InMemoryPlayerRepository(_mockEventBus.Object);

        // A unique connection ID used as the dictionary key for each test player
        _connectionId = new ConnectionId(Guid.NewGuid().ToString());

        _testPlayer = new PlayerState
        {
            ConnId = _connectionId,
            PlayerName = "TestPlayer",
            CurrentLocation = new RoomId("tavern"),
            ActiveConditions = new HashSet<PlayerCondition>()
        };
    }

    // -------------------------------------------------------------------------
    // GetPlayerByConnectionIdAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetPlayerByConnectionId_ReturnsNull_WhenRepositoryIsEmpty()
    {
        var result = await _repository.GetPlayerByConnectionIdAsync(_connectionId);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetPlayerByConnectionId_ReturnsPlayer_AfterUpsert()
    {
        await _repository.UpsertPlayerAsync(_testPlayer);

        var result = await _repository.GetPlayerByConnectionIdAsync(_connectionId);

        Assert.IsNotNull(result);
        Assert.AreEqual("TestPlayer", result.PlayerName);
    }

    // -------------------------------------------------------------------------
    // GetPlayerByNameAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GetPlayerByName_ReturnsNull_WhenPlayerNotFound()
    {
        var result = await _repository.GetPlayerByNameAsync("UnknownPlayer");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetPlayerByName_ReturnsPlayer_WhenPlayerExists()
    {
        await _repository.UpsertPlayerAsync(_testPlayer);

        var result = await _repository.GetPlayerByNameAsync("TestPlayer");

        Assert.IsNotNull(result);
        Assert.AreEqual(_connectionId, result.ConnId);
    }

    // -------------------------------------------------------------------------
    // UpsertPlayerAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task UpsertPlayer_UpdatesExistingPlayer_WhenCalledTwice()
    {
        // Insert the player in the tavern first
        await _repository.UpsertPlayerAsync(_testPlayer);

        // Create an updated version of the same player now located in the forest
        var updatedPlayer = new PlayerState
        {
            ConnId = _connectionId,
            PlayerName = "TestPlayer",
            CurrentLocation = new RoomId("forest"),
            ActiveConditions = new HashSet<PlayerCondition>()
        };

        await _repository.UpsertPlayerAsync(updatedPlayer);

        var result = await _repository.GetPlayerByConnectionIdAsync(_connectionId);
        Assert.AreEqual(new RoomId("forest"), result?.CurrentLocation);
    }

    // -------------------------------------------------------------------------
    // RemovePlayerAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task RemovePlayer_ReturnsTrue_AndRemovesPlayer_WhenPlayerExists()
    {
        await _repository.UpsertPlayerAsync(_testPlayer);

        bool result = await _repository.RemovePlayerAsync(_connectionId);

        Assert.IsTrue(result);

        // Confirm the player is actually gone from the repository
        var player = await _repository.GetPlayerByConnectionIdAsync(_connectionId);
        Assert.IsNull(player);
    }

    [TestMethod]
    public async Task RemovePlayer_ReturnsTrue_WhenPlayerDoesNotExist()
    {
        // The repository treats "not found" as "already removed" and returns true
        bool result = await _repository.RemovePlayerAsync(_connectionId);

        Assert.IsTrue(result);
    }
}
