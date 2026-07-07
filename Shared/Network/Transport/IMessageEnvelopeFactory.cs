using Shared.Identity;
using Shared.Network.Transport;

namespace Shared.Network.Transport
{
    /// <summary>
    /// Factory for creating MessageEnvelopes from deserialized packets.
    /// </summary>
    public interface IMessageEnvelopeFactory
    {
        /// <summary>
        /// Creates a MessageEnvelope from a deserialized packet.
        /// </summary>
        /// <param name="packet">The packet to convert.</param>
        /// <returns>A MessageEnvelope representing the packet's contents.</returns>
        MessageEnvelope CreateFromPacket(MuddyPacket packet, ConnectionId connId);
    }
}
