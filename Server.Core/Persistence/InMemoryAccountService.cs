using Server.Core.Domain.Authentication;
using System.Collections.Concurrent;

namespace Server.Core.Persistence
{
    /// <summary>
    /// Simple in-memory account storage for v1.
    /// Stores username and password pairs.
    /// </summary>
    public class InMemoryAccountService : IAccountService
    {
        private readonly ConcurrentDictionary<string, string> _accounts;

        public InMemoryAccountService()
        {
            _accounts = new ConcurrentDictionary<string, string>();

            // Pre-load some test accounts for testing
            _accounts["Matt"] = "123";
            _accounts["Ahbi"] = "123";
            _accounts["Nasir"] = "123";
            _accounts["Yash"] = "123";
        }

        /// <summary>
        /// Validates login credentials.
        /// </summary>
        /// <returns>True if username and password match</returns>
        public async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            bool userExists = _accounts.TryGetValue(username, out var storedPassword);

            if (!userExists) return await Task.FromResult(false);

            bool passwordMatches = (storedPassword == password);
            return await Task.FromResult(passwordMatches);
        }

        /// <summary>
        /// Registers a new account.
        /// </summary>
        /// <returns>True if account created, false if username already exists</returns>
        public async Task<bool> RegisterAccountAsync(string username, string password)
        {
            bool added = _accounts.TryAdd(username, password);
            return await Task.FromResult(added);
        }

        /// <summary>
        /// Checks if an account exists.
        /// </summary>
        public async Task<bool> AccountExistsAsync(string username)
        {
            bool exists = _accounts.ContainsKey(username);
            return await Task.FromResult(exists);
        }

    }
}
