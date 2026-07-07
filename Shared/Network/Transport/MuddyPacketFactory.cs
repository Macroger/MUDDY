// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.Identity;
using Shared.Network.Types;

namespace Shared.Network.Transport
{
    public class MuddyPacketFactory : IPacketFactory
    {
        public MuddyPacket CreateMuddyPacket(MessageEnvelope message)
        {
            if (message == null) throw new ArgumentNullException("message cannot be null.");

            MuddyPacketHeader newHeader = new MuddyPacketHeader();

            newHeader.SessionId = message.SessionToken?.Value ?? SessionId.Unauthenticated.Value;
            newHeader.BodyLength = (UInt32)message.Payload.Length;
            newHeader.MsgType = (UInt16)message.MessageType;
            newHeader.BitFlags = (UInt16)(message.Flags ?? MessageFlags.None);

            MuddyPacket muddyPacket = new MuddyPacket(newHeader, message.Payload);

            return muddyPacket;
        }
    }
}

