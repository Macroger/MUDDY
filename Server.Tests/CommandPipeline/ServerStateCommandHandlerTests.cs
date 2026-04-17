using Moq;
using Server.Core.CommandPipeline.CommandHandler;
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Types;
using Server.Core.Infrastructure.Lifecycle;

namespace Server.Tests.CommandPipeline;

/// <summary>
/// Unit tests for <see cref="ServerStateCommandHandler"/>.
/// Verifies argument parsing, valid transitions, invalid transitions, and edge cases.
/// </summary>
[TestClass]
public class ServerStateCommandHandlerTests
{
    private Mock<IServerLifecycle> _mockLifecycle = null!;
    private ServerStateCommandHandler _handler = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLifecycle = new Mock<IServerLifecycle>();
        _handler = new ServerStateCommandHandler(_mockLifecycle.Object);
    }

    private static CommandContext BuildContext(params string[] args) =>
        new CommandContext(
            command: new ParsedCommand { CommandType = "serverstate", Arguments = args },
            playerState: null,
            worldState: null,
            success: true,
            errorMessage: null);

    // -------------------------------------------------------------------------
    // Argument validation
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Execute_NoArguments_ReturnsUsageError()
    {
        var result = await _handler.ExecuteAsync(BuildContext());

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Message, "Usage");
    }

    [TestMethod]
    public async Task Execute_UnknownState_ReturnsError()
    {
        var result = await _handler.ExecuteAsync(BuildContext("flying"));

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Message, "flying");
    }

    // -------------------------------------------------------------------------
    // Valid transitions
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Execute_Maintenance_CallsSetState_AndReturnsSuccess()
    {
        _mockLifecycle.Setup(l => l.SetState(ServerStateEnum.MAINTENANCE)).Returns(true);

        var result = await _handler.ExecuteAsync(BuildContext("maintenance"));

        _mockLifecycle.Verify(l => l.SetState(ServerStateEnum.MAINTENANCE), Times.Once);
        Assert.IsTrue(result.Success);
        StringAssert.Contains(result.Message, "MAINTENANCE");
    }

    [TestMethod]
    public async Task Execute_Active_CallsSetState_AndReturnsSuccess()
    {
        _mockLifecycle.Setup(l => l.SetState(ServerStateEnum.ACTIVE)).Returns(true);

        var result = await _handler.ExecuteAsync(BuildContext("active"));

        _mockLifecycle.Verify(l => l.SetState(ServerStateEnum.ACTIVE), Times.Once);
        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public async Task Execute_Shutdown_CallsSetState_AndReturnsSuccess()
    {
        _mockLifecycle.Setup(l => l.SetState(ServerStateEnum.SHUTTING_DOWN)).Returns(true);

        var result = await _handler.ExecuteAsync(BuildContext("shutdown"));

        _mockLifecycle.Verify(l => l.SetState(ServerStateEnum.SHUTTING_DOWN), Times.Once);
        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public async Task Execute_StateArgument_IsCaseInsensitive()
    {
        _mockLifecycle.Setup(l => l.SetState(ServerStateEnum.MAINTENANCE)).Returns(true);

        var result = await _handler.ExecuteAsync(BuildContext("MAINTENANCE"));

        _mockLifecycle.Verify(l => l.SetState(ServerStateEnum.MAINTENANCE), Times.Once);
        Assert.IsTrue(result.Success);
    }

    // -------------------------------------------------------------------------
    // Invalid transitions (SetState returns false)
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Execute_InvalidTransition_ReturnsFailure()
    {
        _mockLifecycle.Setup(l => l.SetState(It.IsAny<ServerStateEnum>())).Returns(false);

        var result = await _handler.ExecuteAsync(BuildContext("active"));

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Message, "Cannot transition");
    }
}
