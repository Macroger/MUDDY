// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.Persistence;

namespace Server.Tests.Persistence;

/// <summary>
/// Unit tests for InMemoryAccountService.
/// Covers credential validation, account registration, and account existence checks.
///
/// Note: InMemoryAccountService pre-loads four test accounts on construction:
///   Matt, Ahbi, Nasir, Yash â€” all with password "123".
/// </summary>
[TestClass]
public class InMemoryAccountServiceTests
{
    private InMemoryAccountService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _service = new InMemoryAccountService();
    }

    // -------------------------------------------------------------------------
    // ValidateCredentialsAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ValidateCredentials_ReturnsTrue_WhenCredentialsAreCorrect()
    {
        // "Matt" is a pre-loaded account with password "123"
        bool result = await _service.ValidateCredentialsAsync("Matt", "123");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ValidateCredentials_ReturnsFalse_WhenPasswordIsWrong()
    {
        bool result = await _service.ValidateCredentialsAsync("Matt", "wrongpassword");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ValidateCredentials_ReturnsFalse_WhenUsernameDoesNotExist()
    {
        bool result = await _service.ValidateCredentialsAsync("UnknownUser", "123");

        Assert.IsFalse(result);
    }

    // -------------------------------------------------------------------------
    // RegisterAccountAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task RegisterAccount_ReturnsTrue_WhenUsernameIsNew()
    {
        bool result = await _service.RegisterAccountAsync("NewPlayer", "password");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task RegisterAccount_ReturnsFalse_WhenUsernameAlreadyExists()
    {
        // "Matt" is a pre-loaded account â€” registering the same name should fail
        bool result = await _service.RegisterAccountAsync("Matt", "newpassword");

        Assert.IsFalse(result);
    }

    // -------------------------------------------------------------------------
    // AccountExistsAsync
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task AccountExists_ReturnsTrue_WhenAccountExists()
    {
        bool result = await _service.AccountExistsAsync("Matt");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task AccountExists_ReturnsFalse_WhenAccountDoesNotExist()
    {
        bool result = await _service.AccountExistsAsync("Nobody");

        Assert.IsFalse(result);
    }

    // -------------------------------------------------------------------------
    // Multi-step scenarios
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task RegisterAccount_ThenValidateCredentials_Succeeds()
    {
        // Register a brand-new account then immediately validate the credentials
        await _service.RegisterAccountAsync("Alice", "securePass");

        bool result = await _service.ValidateCredentialsAsync("Alice", "securePass");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task RegisterAccount_ThenAccountExists_ReturnsTrue()
    {
        await _service.RegisterAccountAsync("Bob", "pass");

        bool exists = await _service.AccountExistsAsync("Bob");

        Assert.IsTrue(exists);
    }
}
