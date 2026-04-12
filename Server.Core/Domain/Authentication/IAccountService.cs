using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Domain.Authentication
{
    /// <summary>
    /// Service for managing player accounts (username/password validation and registration).
    /// </summary>
    public interface IAccountService
    {
        /// <summary>
        /// Validates login credentials.
        /// </summary>
        /// <returns>True if username and password match</returns>
        Task<bool> ValidateCredentialsAsync(string username, string password);

        /// <summary>
        /// Registers a new account.
        /// </summary>
        /// <returns>True if account created, false if username already exists</returns>
        Task<bool> RegisterAccountAsync(string username, string password);

        /// <summary>
        /// Checks if an account exists.
        /// </summary>
        Task<bool> AccountExistsAsync(string username);
    }
}
