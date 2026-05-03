// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.Network.Model;

namespace Server.Core.Network.Listener
{
    public interface IConnectionAcceptedHandler
    {
        void OnConnectionAccepted(AcceptedConnection connection);
    }
}
