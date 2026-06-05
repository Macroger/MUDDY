// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Logging;

namespace Client.Core.Network
{
    /// <summary>
    /// Event raised on the event bus to request a connection to the server.
    /// </summary>
    public sealed record ConnectRequestedEvent() : BusEvent(EventMessageType.Network, LogLevel.Information);

    /// <summary>
    /// Event raised on the event bus to request a disconnect from the server.
    /// </summary>
    public sealed record DisconnectRequestedEvent() : BusEvent(EventMessageType.Network, LogLevel.Information);

    public sealed record ConnectionStatusChangedEvent(bool IsConnected, string Message) : BusEvent(EventMessageType.Network, LogLevel.Information);
}
