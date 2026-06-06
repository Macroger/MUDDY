// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Logging;

namespace Client.Core.Events
{
    public class ClientNetworkEvents
    {
        public class Commands
        {
            /// <summary>
            /// Event raised on the event bus to request a connection to the server.
            /// </summary>
            public sealed record ConnectToServer() : BusEvent(EventMessageType.Network, LogLevel.Information);

            /// <summary>
            /// Event raised on the event bus to request a disconnect from the server.
            /// </summary>
            public sealed record DisconnectFromServer() : BusEvent(EventMessageType.Network, LogLevel.Information);

            public sealed record UpdateConnectionEndpoint(string address, int port) : BusEvent(EventMessageType.Network, LogLevel.Information);
        }

        public sealed record ConnectionStatusChangedEvent(bool IsConnected, string Message) : BusEvent(EventMessageType.Network, LogLevel.Information);

    }    

}
