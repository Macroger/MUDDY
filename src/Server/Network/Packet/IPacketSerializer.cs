using Shared.Protocol.Transport;
using System;

namespace Server.Network.Packet
{
    public interface IPacketSerializer
    {

        /// <summary>
        /// Serializes a MuddyPacket into a contiguous byte array suitable for transmission.
        /// </summary>
        byte[] Serialize(MuddyPacket packet);

        /// <summary>
        /// Attempts to deserialize a MuddyPacket from the provided byte buffer.
        /// Performs validation including size checks and CRC verification.
        /// </summary>
        /// <exception cref="InvalidDataException">
        /// Thrown if the buffer does not represent a valid Muddy packet.
        /// </exception>
        MuddyPacket Deserialize(ReadOnlySpan<byte> buffer);

        public MuddyPacketHeader DeserializeHeader(byte[] serializedHeader);
    }

}
