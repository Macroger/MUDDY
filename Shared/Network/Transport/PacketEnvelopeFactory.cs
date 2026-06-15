using Shared.Identity;
using Shared.Network.Types;

namespace Shared.Network.Transport
{
    /// <summary>
    /// Concrete implementation of IEnvelopeFactory that converts MuddyPackets to PacketEnvelopes.
    /// </summary>
    public sealed class PacketEnvelopeFactory : IPacketEnvelopeFactory
    {
        /// <summary>
        /// Creates a PacketEnvelope from a deserialized MuddyPacket.
        /// </summary>
        /// <param name="packet">The packet to convert.</param>
        /// <returns>A PacketEnvelope with the packet's metadata and body as payload.</returns>
        /// <exception cref="ArgumentNullException">Thrown when packet is null.</exception>
        public PacketEnvelope CreateFromPacket(MuddyPacket packet, ConnectionId connId)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            return new PacketEnvelope(
                messageType: (PacketType)packet.Header.MsgType,
                flags: (MessageFlags)packet.Header.BitFlags,
                payload: packet.Body,
                connectionId: connId,
                sessionId: new SessionId(packet.Header.SessionId)
            );
        }
    }
}