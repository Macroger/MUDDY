using Shared.Protocol.Transport;

namespace Server.Core.Network.Packet
{
    public class MuddyPacketFactory : IPacketFactory
    {
        public MuddyPacket CreateMuddyPacket(TransportEnvelope message)
        {
            if(message == null) throw new ArgumentNullException("message cannot be null.");

            MuddyPacketHeader newHeader = new MuddyPacketHeader();

            newHeader.BodyLength = (UInt32)message.Payload.Length;
            newHeader.MsgId = (UInt32)message.MessageId.Value;
            newHeader.MsgType = (UInt16)message.MessageType;
            newHeader.BitFlags = (UInt16)message.Flags;

            MuddyPacket muddyPacket = new MuddyPacket(newHeader, message.Payload);

            return muddyPacket;
        }
    }
}

