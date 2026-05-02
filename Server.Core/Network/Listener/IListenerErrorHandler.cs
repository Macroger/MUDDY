// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Server.Core.Network.Listener
{
    public interface IListenerErrorHandler
    {
        void OnListenerError(Exception exception);
    }

}
