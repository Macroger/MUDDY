using Server.Core.Domain.Player;
using Shared.EventBus;
using Shared.EventBus.DomainEvents;
using Shared.EventBus.SubscriptionToken;
using Shared.Identity;
using System;
using System.Collections.Concurrent;

namespace Server.Core.Persistence
{
    public class InMemoryPlayerRepository : IPlayerRepository
    {
        private readonly IEventBus _eventBus;
        private readonly ISubscriptionToken _disconnectSubscription;

        /// <summary>
        /// A list of the players that are currently connected to the server. 
        /// This is used to track player state and manage player data during a session.
        /// </summary>
        private ConcurrentDictionary<ConnectionId, PlayerState> _players;

        public InMemoryPlayerRepository(IEventBus eventBus)
        {
            _eventBus = eventBus;
            _players = new ConcurrentDictionary<ConnectionId, PlayerState>();
            _disconnectSubscription = _eventBus.Subscribe<NetworkEvents.ClientDisconnectedEvent>(
                EventMessageType.Network, HandleDisconnect);
        }

        private async void HandleDisconnect(NetworkEvents.ClientDisconnectedEvent evt)
        {
            var player = await GetPlayerByConnectionIdAsync(evt.ConnId);
            if (player is not null)
            {
                _eventBus.Publish(EventMessageType.Domain,
                    new PlayerEvents.PlayerLeftWorldEvent(evt.ConnId, player.CurrentLocation));
                await RemovePlayerAsync(evt.ConnId);
            }
        }

        /// <summary>
        /// Asynchronously retrieves the player state associated with the specified connection identifier.
        /// </summary>
        /// <param name="connId">The unique identifier for the connection whose player state is to be retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the player state associated with
        /// the specified connection identifier, or null if no player is found.</returns>
        public async Task<PlayerState?> GetPlayerByConnectionIdAsync(ConnectionId connId)
        {
            _players.TryGetValue(connId, out PlayerState? player);

            // Wrap the result in a Task to maintain the asynchronous method signature, even though the operation is synchronous.
            return await Task.FromResult(player);
        }

        /// <summary>
        /// Gets a player by their name. This method iterates through the collection of players and returns the first player that matches the provided name.
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        public async Task<PlayerState?> GetPlayerByNameAsync(string playerName)
        {
            foreach (var player in _players.Values) 
            {
                if(player.PlayerName == playerName)
                {
                    return await Task.FromResult(player);
                }
            }

            // Failed to find the player, return a null result wrapped in a Task to maintain the asynchronous method signature.
            return await Task.FromResult<PlayerState?>(null);

        }

        /// <summary>
        /// Update or insert a player's state in the repository.
        /// If the player already exists (identified by their connection ID), their state will be updated with the new information provided. 
        /// If the player does not exist, they will be added to the repository with the provided state.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public async Task UpsertPlayerAsync(PlayerState player)
        {
            _players.AddOrUpdate(
                player.ConnId,                      // Use the player's connection ID as the key for the dictionary.
                player,                             // If the player does not exist in the dictionary, add it with the provided player state.
                (key, existingPlayer) => player     // If the player exists, replace the existing player state with the new one. 
                );
            // Return a completed task to maintain the asynchronous method signature, even though the operation is synchronous.
            await Task.CompletedTask;
        }

        /// <summary>
        /// Attempt to remove a player from the repository based on their connection ID. 
        /// If the player is found and successfully removed, the method returns true; otherwise, it returns false.
        /// </summary>
        /// <param name="connId">The connection Id of the target player.</param>
        /// <returns></returns>
        public async Task<bool> RemovePlayerAsync(ConnectionId connId)
        {
            PlayerState? player = await GetPlayerByConnectionIdAsync(connId);
            if (player == null) 
            {
                // Player not found, return a completed task to maintain the asynchronous method signature.
                return await Task.FromResult(true);
            }
            else
            {
                // Remove the player from the dictionary using the connection ID as the key.
                // The TryRemove method will return true if the player was successfully removed, or false if the player was not found.
                bool result = _players.TryRemove(connId, out _);

                // Return a completed task to maintain the asynchronous method signature, even though the operation is synchronous.
                return await Task.FromResult(result);
            }
        }
    }
}
