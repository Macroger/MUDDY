using Xunit;

namespace Muddy.Tests
{
    public class AuthServiceTests
    {
        [Fact]
        public void Login_ValidCredentials_ReturnsTrue()
        {
            var auth = new AuthService();

            bool result = auth.Login("user", "pass");

            Assert.True(result);
        }

        [Fact]
        public void Login_InvalidCredentials_ReturnsFalse()
        {
            var auth = new AuthService();

            bool result = auth.Login("wrong", "wrong");

            Assert.False(result);
        }
    }

    // Fake implementation (since you don’t have real code)
    public class AuthService
    {
        public bool Login(string username, string password)
        {
            return username == "user" && password == "pass";
        }
    }
}