using Server.Core.Domain.Authentication;
using Server.Core.Infrastructure.Identity.SessionId;
using Shared.EventBus;
using Shared.Identity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Persistence
{
    /// <summary>
    /// An in-memory implementation of <see cref="IAuthenticationService"/> for use in the v1 server.
    /// </summary>
    public class InMemoryAuthenticationService : IAuthenticationService
    {

        private readonly IEventBus _eventBus;
        private readonly ISessionIdGenerator _sessionIdGenerator;

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
        }

        /// <summary>
        /// Creates a new session for the specified connection and player name.
        /// </summary>
        public Task<SessionId> CreateSessionAsync(ConnectionId connId, string playerName)
        {
            var sessionId = _sessionIdGenerator.New();
            var record = new SessionRecord(connId, playerName);

           bool result = _sessions.TryAdd(sessionId, record);

            if(result == false)
            {
                EventReason eventReason = new EventReason
                (
                    Message: $"Failed to create session for player '{playerName}' with connection ID {connId}. Session ID {sessionId} already exists.",
                    Data: new { PlayerName = playerName, ConnectionId = connId, SessionId = sessionId }
                );

                EventBusHelper.PublishEvent(
                    _eventBus, 
                    EventMessageType.Error,
                    eventReason
                    );
            }
            else
            {
                EventReason eventReason = new EventReason
                (
                    Message: $"Session created for player '{playerName}' with connection ID {connId}. Assigned session ID {sessionId}.",
                    Data: new { PlayerName = playerName, ConnectionId = connId, SessionId = sessionId }
                );

                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Authentication,
                    eventReason
                    );
            }

            return Task.FromResult(sessionId);
        }

        /// <summary>
        /// Removes the session associated with the specified session ID.
        /// </summary>
        public Task RemoveSession(SessionId sessionId)
        {
            bool result = _sessions.TryRemove(sessionId, out _);

            string logMessage = result ? $"Session with ID {sessionId} removed successfully."
                : $"Failed to remove session with ID {sessionId}. Session not found.";

            EventReason eventReason = new EventReason
            (
                Message: logMessage,
                Data: new { SessionId = sessionId }
            );

            EventBusHelper.PublishEvent(
                _eventBus,
                result ? EventMessageType.Authentication : EventMessageType.Error,
                eventReason
            );

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
    }
}
