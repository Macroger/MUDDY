// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.Domain.Authentication;
using Server.Core.Infrastructure.Events;
using Server.Core.Infrastructure.Identity.SessionId;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;
using Shared.Identity;
using System.Collections.Concurrent;

namespace Server.Core.Persistence
{
    /// <summary>
    /// An in-memory implementation of <see cref="IAuthenticationService"/> for use in the v1 server.
    /// </summary>
    public class InMemoryAuthenticationService : IAuthenticationService
    {

        private readonly IEventBus _eventBus;
        private readonly ISessionIdGenerator _sessionIdGenerator;
        private readonly ISubscriptionToken _playerLeftSubscription;

        /// <summary>
        /// Represents a stored session record with connection and player information.
        /// </summary>
        private sealed record SessionRecord(ConnectionId ConnId, string PlayerName);

        /// <summary>
        /// Active sessions keyed by <see cref="SessionId"/>.
        /// </summary>
        private readonly ConcurrentDictionary<SessionId, SessionRecord> _sessions = new();


        /// <summary>
        /// Counter for generating unique session IDs. Starts at 1 since 0 is reserved for <see cref="SessionId.Unauthenticated"/>.
        /// </summary>
        //private long _nextSessionId = 1;

        /// <summary>
        /// Constructor for InMemoryAuthenticationService that takes an event bus for publishing authentication-related events.
        /// </summary>
        /// <param name="eventBus"></param>
        public InMemoryAuthenticationService(IEventBus eventBus, ISessionIdGenerator sessionIdGenerator)
        {
            _eventBus = eventBus;
            _sessionIdGenerator = sessionIdGenerator;
            _playerLeftSubscription = _eventBus.Subscribe<WorldEvents.Notifications.PlayerLeftWorldEvent>(
                EventMessageType.World,
                async evt => await RemoveSessionByConnectionIdAsync(evt.ConnId));
        }

        /// <summary>
        /// Creates a new session for the specified connection and player name.
        /// </summary>
        public Task<SessionId> CreateSessionAsync(ConnectionId connId, string playerName)
        {
            var sessionId = _sessionIdGenerator.New();
            var record = new SessionRecord(connId, playerName);

            bool result = _sessions.TryAdd(sessionId, record);

            if (result == false)
            {

                _eventBus.Publish(
                    EventMessageType.System,
                    new SystemEvents.Errors.SystemErrorEvent($"Failed to create session for player '{playerName}' with connection ID {connId}. Session ID {sessionId} already exists."));
                
            }
            else
            {
                _eventBus.Publish(
                    EventMessageType.Authentication,
                    new AuthenticationEvents.Notifications.SessionCreatedEvent($"Successfully created session ID for {playerName} at connection: {connId}."));
            }

            return Task.FromResult(sessionId);
        }

        /// <summary>
        /// Removes the session associated with the specified session ID.
        /// </summary>
        public Task RemoveSession(SessionId sessionId)
        {
            bool result = _sessions.TryRemove(sessionId, out _);
            string logMessage = "";
            if (result == true)
            {
                logMessage = $"Session with ID {sessionId} removed successfully.";

                _eventBus.Publish(
                EventMessageType.Authentication,
                new AuthenticationEvents.Notifications.SessionRemovedEvent(logMessage));
            }
            else
            {
                logMessage = $"Failed to remove session with ID {sessionId}. Session not found.";

                _eventBus.Publish(
                EventMessageType.System,
                new SystemEvents.Errors.SystemErrorEvent(logMessage));
            }

            

            return Task.CompletedTask;
        }

        /// <summary>
        /// Validates that the specified session exists and belongs to the specified connection.
        /// </summary>
        public Task<bool> ValidateSessionAsync(SessionId? sessionId, ConnectionId connId)
        {
            // Reject null or unauthenticated session tokens immediately
            if (sessionId is null || sessionId.Value == SessionId.Unauthenticated)
                return Task.FromResult(false);

            // Validate session exists and belongs to the specified connection
            if (_sessions.TryGetValue(sessionId.Value, out var record))
                return Task.FromResult(record.ConnId.Equals(connId));

            return Task.FromResult(false);
        }

        public Task RemoveSessionByConnectionIdAsync(ConnectionId connId)
        {
            var match = _sessions.FirstOrDefault(kv => kv.Value.ConnId == connId);
            bool result = false;

            // Check if the session was found before attempting to remove, and log appropriately
            if (match.Key != default)
            {
                result = _sessions.TryRemove(match.Key, out _);
            }


            if (result == true)
            {
                _eventBus.Publish(EventMessageType.Authentication,
                new AuthenticationEvents.Notifications.SessionRemovedEvent($"Session with ID {match.Key} removed successfully for connection ID {connId}."));
            }
            else
            {
                _eventBus.Publish(EventMessageType.Authentication,
                new AuthenticationEvents.Notifications.SessionRemovedEvent($"Failed to remove session for connection ID {connId}. No matching session found."));
                
            }

            return Task.CompletedTask;
        }

    }
}
