using Shared.Identity;
using Shared.Network.Transport;

namespace Shared.Network.Transport
{
    /// <summary>
    /// Factory for creating PacketEnvelopes from deserialized packets.
    /// </summary>
    public interface IPacketEnvelopeFactory
    {
        /// <summary>
        /// Creates a PacketEnvelope from a deserialized packet.
        /// </summary>
        /// <param name="packet">The packet to convert.</param>
        /// <returns>A PacketEnvelope representing the packet's contents.</returns>
        PacketEnvelope CreateFromPacket(MuddyPacket packet, ConnectionId connId);
    }
}
