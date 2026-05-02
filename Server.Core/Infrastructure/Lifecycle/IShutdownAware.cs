// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Server.Core.Infrastructure.Lifecycle
{
    /// <summary>
    /// An interface used to mark items as shutdown aware, meaning they have a Shutdown method that can
    /// be called to perform any necessary cleanup or finalization before the application is shut down.
    /// </summary>
    public interface IShutdownAware
    {
        void OnShutdown();
    }
}
