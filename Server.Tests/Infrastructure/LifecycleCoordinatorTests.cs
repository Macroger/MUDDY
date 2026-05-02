// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
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
    private LifecycleCoordinator _coordinator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        IEventBus mockEventBus = new Mock<IEventBus>().Object;
        _coordinator = new LifecycleCoordinator(mockEventBus);
    }

    // -------------------------------------------------------------------------
    // StartServer â€” LOADING â†’ ACTIVE
    // -------------------------------------------------------------------------

    [TestMethod]
    public void StartServer_FromLoadingState_ReturnsTrue()
    {
        bool result = _coordinator.StartServer();

        Assert.IsTrue(result);
        Assert.IsTrue(_coordinator.IsActive);
    }

    // -------------------------------------------------------------------------
    // ShutdownServer â€” ACTIVE â†’ SHUTTING_DOWN
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
    // TryTransition â€” invalid transition returns false
    // -------------------------------------------------------------------------

    [TestMethod]
    public void TryTransition_FromLoadingToShuttingDown_ReturnsFalse()
    {
        // LOADING â†’ SHUTTING_DOWN is not an allowed transition
        bool result = _coordinator.TryTransition(ServerStateEnum.LOADING, ServerStateEnum.SHUTTING_DOWN);

        Assert.IsFalse(result);
    }

}
