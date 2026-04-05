using Shared.Protocol.Transport;
using Shared.Protocol.Types;

namespace Server.Core.Network.Packet
{
    /// <summary>
    /// Creates a MuddyPacket representing the given message.
    /// Currently, each message maps to exactly one packet.
    /// </summary>
    public interface IPacketFactory
    {
        MuddyPacket CreateMuddyPacket(Shared.Protocol.Types.ProtocolEnvelope message);
    }
}
