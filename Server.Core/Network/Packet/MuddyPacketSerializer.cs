using Shared.Protocol.Transport;
using System.Buffers.Binary;
using System.Collections.Specialized;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server.Core.Network.Packet
{
    public class MuddyPacketSerializer : IPacketSerializer
    {
        private readonly MuddyProtocolLimits packetLimits;
        private readonly int headerSize;
        private readonly int tailSize;

        public MuddyPacketSerializer(MuddyProtocolLimits limits)
        {
            packetLimits = limits;
            headerSize = packetLimits.headerSize;
            tailSize = packetLimits.tailSize;
        }


        /// <summary>
        /// Deserializes a binary packet into a new instance of the MuddyPacket class.
        /// </summary>
        /// <remarks>The method validates the packet's integrity by checking its length and CRC value. If
        /// the packet is invalid or corrupted, an exception is thrown.</remarks>
        /// <param name="buffer">A read-only span of bytes containing the serialized packet data. The span must include both the header and
        /// body, followed by a CRC value.</param>
        /// <returns>A MuddyPacket instance representing the deserialized packet, including header, body, and CRC information.</returns>
        /// <exception cref="InvalidDataException">Thrown if the packet is truncated, malformed, or if the CRC check fails, indicating corrupted data.</exception>

        public MuddyPacket Deserialize(ReadOnlySpan<byte> buffer)
        {
            // Validate that the serialized packet is large enough to contain at least the header and CRC.
            if (buffer.Length < (headerSize + tailSize))
                throw new InvalidDataException($"Packet truncated or malformed; expected at least {headerSize + tailSize} bytes, got {buffer.Length}.");

            // Validate that the serialized packet does not exceed the maximum allowed packet size.
            if (buffer.Length > packetLimits.MaxPacketBytes)
                throw new InvalidDataException($"Packet too large; exceeds body size limit of {packetLimits.MaxPacketBytes} bytes, got {buffer.Length}.");
            
            // Create a span targeting the header portion of the serialized packet.
            ReadOnlySpan<byte> headerSpan = buffer.Slice(0, headerSize);

            // Deserialize the header fields from the header span using BinaryPrimitives.ReadUIntx methods.
            // Create a new header object to hold the deserialized header values.
            MuddyPacketHeader newHeader = DeserializeHeader(buffer[..headerSize]);     

            int expectedSize = headerSize + (int)newHeader.BodyLength + tailSize;

            if (buffer.Length != expectedSize)
                throw new InvalidDataException($"Packet length mismatch. Buffer does not equal expected size for packet. Packet is: {buffer.Length} of expected: {expectedSize}");

            // Validate length now that we have deserialized it
            if (buffer.Length < (headerSize + newHeader.BodyLength + tailSize))
                throw new InvalidDataException("Packet truncated or malformed. Packet body is smaller then expected.");

            // Check if the body size exceeds the maximum allowed JSON body size, which is calculated by subtracting the header and tail sizes from the total packet size.
            if (newHeader.BodyLength > packetLimits.MaxJsonBodyBytes)
                throw new InvalidDataException($"Packet malformed; exceeds JSON body size limit of {packetLimits.MaxJsonBodyBytes} bytes, got {newHeader.BodyLength} bytes.");

            // Create a span targeting the body.
            ReadOnlySpan<byte> bodySpan = buffer.Slice(headerSize, (int)newHeader.BodyLength);

            // Create a span targeting the Crc value.
            ReadOnlySpan<byte> crcSpan = buffer.Slice((headerSize + (int)newHeader.BodyLength), 4);

            // Copy the body span into a new byte array, since the body is stored as a byte array in the class.
            byte[] newBody = new byte[newHeader.BodyLength];
            bodySpan.CopyTo(newBody);

            // Read the Crc value from the crc span, and calculate the Crc value based on the header and body.
            UInt32 receivedCrC = BinaryPrimitives.ReadUInt32LittleEndian(crcSpan);
            UInt32 calculatedCrc = CalculateCrc(headerSpan, newBody);

            // Check if the Crc values match. 
            if (calculatedCrc != receivedCrC)
                throw new InvalidDataException($"CRC mismatch; Packet corrupted.");

            return new MuddyPacket(newHeader, newBody, receivedCrC);
        }

        /// <summary>
        /// Serializes the current packet into a byte array suitable for transmission or storage.
        /// </summary>
        /// <remarks>The returned array contains the packet data in the order: header, body, and CRC. The
        /// CRC is written in little-endian format. The caller is responsible for ensuring the packet is used in a
        /// context that expects this format.</remarks>
        /// <returns>A byte array containing the serialized packet, including header, body, and CRC segments.</returns>

        public byte[] Serialize(MuddyPacket incommingPkt)
        {
            // Create a set of variables to hold the packet segment sizes (mostly for convenience).
            int bodySize = (int)incommingPkt.Header.BodyLength;
            int headerSize = packetLimits.headerSize;
            int tailSize = packetLimits.tailSize;
            int packetSize = headerSize + bodySize + tailSize;

            if (incommingPkt.Header.BodyLength != incommingPkt.Body.Length)
                throw new InvalidDataException($"Packet body length mismatch; expected {incommingPkt.Header.BodyLength} bytes, got {incommingPkt.Body.Length} bytes.");

            // Serialize the header into a byte array and calculate the CRC value for the combined header and body.
            byte[] serializedHeader = SerializeHeader(incommingPkt.Header);
            uint crc = CalculateCrc(serializedHeader, incommingPkt.Body);

            // Create a Span of bytes large enough to hold the entire packet.
            Span<byte> packet = new byte[packetSize];

            // Section off parts of the span for the segments.
            // This is where we dictate the order of the segments in the final packet.
            Span<byte> headerSpan = packet.Slice(0, length: headerSize);
            Span<byte> bodySpan = packet.Slice(start: headerSize, length: bodySize);
            Span<byte> crcSpan = packet.Slice(start: (headerSize + bodySize), length: tailSize);

            // Copy the serialized header, body, and CRC into their respective spans.
            serializedHeader.AsSpan().CopyTo(headerSpan);
            incommingPkt.Body.AsSpan().CopyTo(bodySpan);
            BinaryPrimitives.WriteUInt32LittleEndian(crcSpan, crc);

            // Return the newly built packet as a byte array.
            return packet.ToArray();
        }


        /// <summary>
        /// Calculates the CRC32 checksum for the combined header and body byte arrays.
        /// </summary>
        /// <remarks>The CRC32 checksum is used to verify data integrity. The method processes
        /// the header and body sequentially, as if they were concatenated.</remarks>
        /// <param name="header">The byte array representing the header data to include in the CRC calculation. Cannot be null.</param>
        /// <param name="body">The byte array representing the body data to include in the CRC calculation. Cannot be null.</param>
        /// <returns>A 32-bit unsigned integer containing the CRC32 checksum of the combined header and body data.</returns>
        private static UInt32 CalculateCrc(ReadOnlySpan<byte> header, ReadOnlySpan<byte> body)
        {
            // Create a new Crc32 instance to compute the checksum.
            Crc32 crc = new();

            // Append the header and body byte arrays to the CRC calculator. The order of appending should match the order of serialization.
            crc.Append(header);
            crc.Append(body);

            // Return the computed CRC value as a UInt32.
            return crc.GetCurrentHashAsUInt32();
        }

        /// <summary>
        /// Serializes the specified packet header into a byte array using little-endian encoding.
        /// </summary>
        /// <remarks>The returned byte array can be used for network transmission or storage. All header
        /// fields are encoded in little-endian format. The caller is responsible for ensuring the header fields are
        /// valid and consistent with the expected protocol.</remarks>
        /// <param name="header">The packet header to serialize. Must contain valid field values for all header components.</param>
        /// <returns>A byte array containing the serialized representation of the header. The array length matches the header
        /// size.</returns>
        private byte[] SerializeHeader(MuddyPacketHeader header)
        {
            // Create a Span {headerByteSize} bytes in length.
            Span<byte> serializedHeader = stackalloc byte[packetLimits.headerSize];

            // Copy the header fields into the serializedHeader via BinaryPrimitives.WriteUIntx
            WriteSessionId(serializedHeader.Slice(MuddyPacketHeader.SessionIdOffset, MuddyPacketHeader.SessionIdSize), header.SessionId);
            BinaryPrimitives.WriteUInt32LittleEndian(serializedHeader.Slice(MuddyPacketHeader.BodyLengthOffset, MuddyPacketHeader.BodyLengthSize), header.BodyLength);
            BinaryPrimitives.WriteUInt32LittleEndian(serializedHeader.Slice(MuddyPacketHeader.MsgIdOffset, MuddyPacketHeader.MsgIdSize), header.MsgId);
            BinaryPrimitives.WriteUInt16LittleEndian(serializedHeader.Slice(MuddyPacketHeader.MsgTypeOffset, MuddyPacketHeader.MsgTypeSize), header.MsgType);
            BinaryPrimitives.WriteUInt16LittleEndian(serializedHeader.Slice(MuddyPacketHeader.BitFlagsOffset, MuddyPacketHeader.BitFlagsSize), header.BitFlags);

            return serializedHeader.ToArray();
        }

        public MuddyPacketHeader DeserializeHeader(ReadOnlySpan<byte> serializedHeader)
        {
            if (serializedHeader.Length != packetLimits.headerSize)
                throw new InvalidDataException($"Serialized header size mismatch; expected {packetLimits.headerSize} bytes, got {serializedHeader.Length} bytes.");

            MuddyPacketHeader newHeader = new MuddyPacketHeader();
            ReadOnlySpan<byte> headerSpan = serializedHeader;

            newHeader.SessionId = ReadSessionId(headerSpan.Slice(MuddyPacketHeader.SessionIdOffset, MuddyPacketHeader.SessionIdSize));
            newHeader.BodyLength = BinaryPrimitives.ReadUInt32LittleEndian(headerSpan.Slice(MuddyPacketHeader.BodyLengthOffset, MuddyPacketHeader.BodyLengthSize));
            newHeader.MsgId = BinaryPrimitives.ReadUInt32LittleEndian(headerSpan.Slice(MuddyPacketHeader.MsgIdOffset, MuddyPacketHeader.MsgIdSize));
            newHeader.MsgType = BinaryPrimitives.ReadUInt16LittleEndian(headerSpan.Slice(MuddyPacketHeader.MsgTypeOffset, MuddyPacketHeader.MsgTypeSize));
            newHeader.BitFlags = BinaryPrimitives.ReadUInt16LittleEndian(headerSpan.Slice(MuddyPacketHeader.BitFlagsOffset, MuddyPacketHeader.BitFlagsSize));

            return newHeader;
        }

        private static void WriteSessionId(Span<byte> buffer, Guid sessionId)
        {
            MemoryMarshal.Write(buffer, sessionId);
        }

        private static Guid ReadSessionId(ReadOnlySpan<byte> buffer)
        {
            return MemoryMarshal.Read<Guid>(buffer);
        }
    }
}
