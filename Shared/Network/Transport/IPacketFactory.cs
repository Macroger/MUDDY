// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Shared.Protocol.Transport
{
    /// <summary>
    /// Creates a MuddyPacket representing the given message.
    /// Currently, each message maps to exactly one packet.
    /// </summary>
    public interface IPacketFactory
    {
        MuddyPacket CreateMuddyPacket(TransportEnvelope message);
    }
}
