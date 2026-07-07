using Shared.Identity;
using Shared.Network.Types;

namespace Shared.Network.Transport
{
    /// <summary>
    /// Concrete implementation of IEnvelopeFactory that converts MuddyPackets to PacketEnvelopes.
    /// </summary>
    public sealed class MessageEnvelopeFactory : IMessageEnvelopeFactory
    {
        /// <summary>
        /// Creates a MessageEnvelope from a deserialized MuddyPacket.
        /// </summary>
        /// <param name="packet">The packet to convert.</param>
        /// <returns>A MessageEnvelope with the packet's metadata and body as payload.</returns>
        /// <exception cref="ArgumentNullException">Thrown when packet is null.</exception>
        public MessageEnvelope CreateFromPacket(MuddyPacket packet, ConnectionId connId)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            return new MessageEnvelope(
                messageType: (PacketType)packet.Header.MsgType,
                flags: (MessageFlags)packet.Header.BitFlags,
                payload: packet.Body,
                connectionId: connId,
                sessionId: new SessionId(packet.Header.SessionId)
            );
        }
    }
}