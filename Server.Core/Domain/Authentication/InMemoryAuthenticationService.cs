using Shared.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Domain.Authentication
{
    internal class InMemoryAuthenticationService : IAuthenticationService
    {
        public Task<SessionId> CreateSessionAsync(ConnectionId connId, string playerName)
        {
            throw new NotImplementedException();
        }

        public Task RemoveSession(SessionId sessionId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateSessionAsync(SessionId sessionId, ConnectionId connId)
        {
            throw new NotImplementedException();
        }
    }
}
