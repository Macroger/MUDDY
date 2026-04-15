using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Policies;
using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.Player;
using Shared.Identity;

namespace Server.Tests.CommandPipeline;

/// <summary>
/// Unit tests for MutedPlayerPolicy.
/// Covers that muted players are blocked from "say" and all other players are allowed.
/// </summary>
[TestClass]
public class MutedPlayerPolicyTests
{
    private MutedPlayerPolicy _policy;

    [TestInitialize]
    public void TestInitialize()
    {
        _policy = new MutedPlayerPolicy();
    }

    private static PlayerState BuildPlayer(bool muted)
    {
        var conditions = new HashSet<PlayerCondition>();
        if (muted) conditions.Add(PlayerCondition.Muted);

        return new PlayerState
        {
            ConnId = new ConnectionId(Guid.NewGuid().ToString()),
            PlayerName = "TestPlayer",
            CurrentLocation = new RoomId("tavern"),
            ActiveConditions = conditions
        };
    }

    private static CommandContext BuildContext(PlayerState player, string verb)
    {
        var command = new ParsedCommand
        {
            CommandType = verb,
            Arguments = Array.Empty<string>(),
            MsgId = new MessageId(1)
        };
        return new CommandContext(command, player, null, true, null);
    }

    // -------------------------------------------------------------------------
    // Muted player — say command blocked
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CheckPolicy_MutedPlayerSayCommand_ReturnsFailure()
    {
        var context = BuildContext(BuildPlayer(muted: true), "say");

        var result = await _policy.CheckPolicyAsync(context);

        Assert.IsFalse(result.IsValid);
        Assert.IsNotNull(result.ErrorMessage);
    }

    // -------------------------------------------------------------------------
    // Unmuted player — say command allowed
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CheckPolicy_UnmutedPlayerSayCommand_ReturnsSuccess()
    {
        var context = BuildContext(BuildPlayer(muted: false), "say");

        var result = await _policy.CheckPolicyAsync(context);

        Assert.IsTrue(result.IsValid);
    }

    // -------------------------------------------------------------------------
    // Muted player — non-say command still allowed
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task CheckPolicy_MutedPlayerNonSayCommand_ReturnsSuccess()
    {
        var context = BuildContext(BuildPlayer(muted: true), "move");

        var result = await _policy.CheckPolicyAsync(context);

        Assert.IsTrue(result.IsValid);
    }
}
