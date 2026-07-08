namespace Shared.Network.Transport
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

        /// <summary>
        /// Deserializes a packet header from the specified read-only span of bytes.
        /// </summary>
        /// <param name="serializedHeader">A read-only span of bytes containing the serialized packet header data to deserialize.</param>
        /// <returns>A MuddyPacketHeader instance representing the deserialized header data.</returns>
        public MuddyPacketHeader DeserializeHeader(ReadOnlySpan<byte> serializedHeader);
    }

}
