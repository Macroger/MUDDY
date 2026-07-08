using Shared.Identity;

namespace Shared.Network.Transport
{
    /// <summary>
    /// Factory for creating MessageEnvelopes from deserialized packets.
    /// </summary>
    public interface IMessageEnvelopeFactory
    {
        /// <summary>
        /// Creates a PacketEnvelope from a deserialized packet.
        /// </summary>
        /// <param name="packet">The packet to convert.</param>
        /// <returns>A PacketEnvelope representing the packet's contents.</returns>
        PacketEnvelope CreateFromPacket(MuddyPacket packet, ConnectionId connId);
    }
}
