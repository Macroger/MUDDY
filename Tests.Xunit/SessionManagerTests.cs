using Xunit;
using System.Collections.Generic;

namespace Muddy.Tests
{
    public class SessionManagerTests
    {
        [Fact]
        public void CreateSession_ReturnsValidSessionId()
        {
            var manager = new SessionManager();

            var sessionId = manager.CreateSession("user");

            Assert.True(sessionId > 0);
        }

        [Fact]
        public void Validate_InvalidSession_ReturnsFalse()
        {
            var manager = new SessionManager();

            bool isValid = manager.Validate(999);

            Assert.False(isValid);
        }
    }

    public class SessionManager
    {
        private Dictionary<int, string> sessions = new Dictionary<int, string>();
        private int currentId = 1;

        public int CreateSession(string username)
        {
            sessions[currentId] = username;
            return currentId++;
        }

        public bool Validate(int sessionId)
        {
            return sessions.ContainsKey(sessionId);
        }
    }
}