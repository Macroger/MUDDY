using Moq;
using Server.Core.Infrastructure.Lifecycle;
using Shared.EventBus;

namespace Server.Tests.Infrastructure;

/// <summary>
/// Unit tests for LifecycleCoordinator.
/// Covers valid state transitions, guard against invalid transitions, and event firing.
/// </summary>
[TestClass]
public class LifecycleCoordinatorTests
{
    private LifecycleCoordinator _coordinator;

    [TestInitialize]
    public void TestInitialize()
    {
        IEventBus mockEventBus = new Mock<IEventBus>().Object;
        _coordinator = new LifecycleCoordinator(mockEventBus);
    }

    // -------------------------------------------------------------------------
    // StartServer — LOADING → ACTIVE
    // -------------------------------------------------------------------------

    [TestMethod]
    public void StartServer_FromLoadingState_ReturnsTrue()
    {
        bool result = _coordinator.StartServer();

        Assert.IsTrue(result);
        Assert.IsTrue(_coordinator.IsActive);
    }

    // -------------------------------------------------------------------------
    // ShutdownServer — ACTIVE → SHUTTING_DOWN
    // -------------------------------------------------------------------------

    [TestMethod]
    public void ShutdownServer_FromActiveState_ReturnsTrue()
    {
        _coordinator.StartServer();

        bool result = _coordinator.ShutdownServer();

        Assert.IsTrue(result);
        Assert.IsTrue(_coordinator.IsShuttingDown);
    }

    // -------------------------------------------------------------------------
    // TryTransition — invalid transition returns false
    // -------------------------------------------------------------------------

    [TestMethod]
    public void TryTransition_FromLoadingToShuttingDown_ReturnsFalse()
    {
        // LOADING → SHUTTING_DOWN is not an allowed transition
        bool result = _coordinator.TryTransition(ServerStateEnum.LOADING, ServerStateEnum.SHUTTING_DOWN);

        Assert.IsFalse(result);
    }

    // -------------------------------------------------------------------------
    // StateChanged event fires on valid transition
    // -------------------------------------------------------------------------

    [TestMethod]
    public void StartServer_FiresStateChangedEvent()
    {
        ServerStateChangedEvent? captured = null;
        _coordinator.StateChanged += (_, e) => captured = e;

        _coordinator.StartServer();

        Assert.IsNotNull(captured);
        Assert.AreEqual(ServerStateEnum.ACTIVE, captured.NewState);
    }
}
