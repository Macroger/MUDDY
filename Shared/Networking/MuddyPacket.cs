using System.IO.Hashing;
using System.Buffers.Binary;
using System.Text;

namespace Shared.Networking
{

    /// <summary>
    /// Represents the header information for a Muddy protocol packet, including session identification, message
    /// metadata, and control flags.
    /// </summary>
    /// <remarks>Use this structure to describe the essential metadata required for processing and routing
    /// MuddyPacket messages. The fields provide information about the session, message type, and additional control
    /// flags that may influence packet handling. This struct is typically used in network communication scenarios where
    /// Muddy protocol packets are serialized and deserialized.</remarks>
    public struct MuddyPacketHeader
    {
        public UInt64 SessionId;
        public UInt32 BodyLength;
        public UInt32 MsgId;
        public UInt16 MsgType;
        public UInt16 BitFlags;
    }

    /// <summary>
    /// Represents a network packet containing a header, body, and CRC for integrity verification.
    /// </summary>
    /// <remarks>The packet can be constructed from either a JSON string or binary data, and includes a header
    /// with metadata such as session ID, message ID, type, and flags. Use the static Deserialize method to reconstruct
    /// a packet from a serialized byte span. The class enforces maximum body size limits and validates CRC to ensure
    /// data integrity. Exceptions are thrown if the packet is malformed or corrupted.</remarks>
    public class MuddyPacket
    {
        private readonly MuddyPacketHeader header;  // Store the header as a struct for easy access to its fields.
        private readonly byte[] serializedHeader;   // Store the header as a byte array for easy serialization and CRC calculation.
        private readonly byte[] serializedBody;     // Store the body as a byte array, since it can be either JSON or binary data.
        private readonly UInt32 crc;                // Store the CRC value for integrity verification.

        private const int headerSize = 20; // The size in bytes of the header.
        private readonly int bodySize;       // The size in bytes of the body, as specified in the header.
        private const int tailSize = 4;     // The size in bytes of the CRC value at the end of the packet.
        private const int maxJSONBodySize = 16384; // Set max JSON body size to 16 KiloBytes.
        private const int maxBinaryBlobSize = 2097152; // Set max binary blob size to 2 MegaBytes.

        public MuddyPacket(UInt32 msgId,
            UInt16 msgType,
            UInt16 bitFlags,
            String body,
            UInt64 sessionId = 0)
        {
            // First, check if body size is acceptable.
            // Only check for JSON body size, since binary blobs are not implemented yet.
            if (body.Length > maxJSONBodySize)
                throw new ArgumentException($"Body size exceeds maximum allowed of {maxJSONBodySize} bytes.");

            // Setup a new header object - to be filled in with incoming values.
            MuddyPacketHeader newHeader = new MuddyPacketHeader();

            // Assign values to the new header object.
            newHeader.SessionId = sessionId;
            newHeader.MsgId = msgId;
            newHeader.MsgType = msgType;
            newHeader.BitFlags = bitFlags;
            newHeader.BodyLength = (UInt32) body.Length;    // Cast to UInt32 - since body length should never be negative, is safe

            // Assign the new header and body to the class fields.
            header = newHeader;
            bodySize = (int) newHeader.BodyLength;
            serializedHeader = SerializeHeader(newHeader);
            serializedBody = Encoding.UTF8.GetBytes(body);

            // Calculate crc value based on the header and body.
            crc = CalculateCrc(serializedHeader, serializedBody);
        }

        /// <summary>
        /// Initializes a new instance of the MuddyPacket class with the specified header, body, and CRC value.
        /// </summary>
        /// <remarks>Use this constructor when you already have the header, body, and CRC values available, such as when
        /// deserializing a packet from a byte array.</remarks>
        /// <param name="header">The header information for the packet, containing metadata such as type and length.</param>
        /// <param name="body">The byte array representing the packet's body content. Cannot be null.</param>
        /// <param name="crc">The CRC32 checksum used to verify the integrity of the packet data.</param>
        public MuddyPacket(MuddyPacketHeader header, byte[] body, UInt32 crc)
        {
            this.header = header;
            this.serializedHeader = SerializeHeader(header);
            this.serializedBody = body;
            this.crc = crc;
        }

        /// <summary>
        /// Calculates the CRC32 checksum for the combined header and body byte arrays.
        /// </summary>
        /// <remarks>The CRC32 checksum is used to verify data integrity. The method processes
        /// the header and body sequentially, as if they were concatenated.</remarks>
        /// <param name="header">The byte array representing the header data to include in the CRC calculation. Cannot be null.</param>
        /// <param name="body">The byte array representing the body data to include in the CRC calculation. Cannot be null.</param>
        /// <returns>A 32-bit unsigned integer containing the CRC32 checksum of the combined header and body data.</returns>
        private static UInt32 CalculateCrc(byte[] header, byte[] body)
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
        private static byte[] SerializeHeader(MuddyPacketHeader header)
        {
            // Create a Span {headerByteSize} bytes in length.
            Span<byte> serializedHeader = stackalloc byte[headerSize];

            // Copy the header fields into the serializedHeader via BinaryPrimitives.WriteUIntx
            BinaryPrimitives.WriteUInt64LittleEndian(serializedHeader, header.SessionId);
            BinaryPrimitives.WriteUInt32LittleEndian(serializedHeader, header.BodyLength);
            BinaryPrimitives.WriteUInt32LittleEndian(serializedHeader, header.MsgId);
            BinaryPrimitives.WriteUInt16LittleEndian(serializedHeader, header.MsgType);
            BinaryPrimitives.WriteUInt16LittleEndian(serializedHeader, header.BitFlags);

            return serializedHeader.ToArray();    
        }

        /// <summary>
        /// Serializes the current packet into a byte array suitable for transmission or storage.
        /// </summary>
        /// <remarks>The returned array contains the packet data in the order: header, body, and CRC. The
        /// CRC is written in little-endian format. The caller is responsible for ensuring the packet is used in a
        /// context that expects this format.</remarks>
        /// <returns>A byte array containing the serialized packet, including header, body, and CRC segments.</returns>
        public byte[] Serialize()
        {
            // Create a set of variables to hold the packet segment sizes (mostly for convenience).
            int bodySize = (int)header.BodyLength;
           
            int packetSize = headerSize + bodySize + tailSize;

            // Create a Span bytes large enough to hold the entire packet.
            Span<byte> packet = new byte[packetSize];

            // Section off parts of the span for the segments
            Span<byte> headerSpan = packet.Slice(0, headerSize);
            Span<byte> bodySpan = packet.Slice(headerSize, bodySize);
            Span<byte> crcSpan = packet.Slice((headerSize + bodySize), tailSize);

            serializedHeader.AsSpan().CopyTo(headerSpan);
            serializedBody.AsSpan().CopyTo(bodySpan);

            BinaryPrimitives.WriteUInt32LittleEndian(crcSpan, crc);

            return packet.ToArray();
        }

        /// <summary>
        /// Deserializes a binary packet into a new instance of the MuddyPacket class.
        /// </summary>
        /// <remarks>The method validates the packet's integrity by checking its length and CRC value. If
        /// the packet is invalid or corrupted, an exception is thrown.</remarks>
        /// <param name="serializedPacket">A read-only span of bytes containing the serialized packet data. The span must include both the header and
        /// body, followed by a CRC value.</param>
        /// <returns>A MuddyPacket instance representing the deserialized packet, including header, body, and CRC information.</returns>
        /// <exception cref="InvalidDataException">Thrown if the packet is truncated, malformed, or if the CRC check fails, indicating corrupted data.</exception>
        public static MuddyPacket Deserialize(ReadOnlySpan<byte> serializedPacket)
        {
            // Create a new header object to hold the deserialized header values.
            MuddyPacketHeader newHeader = new MuddyPacketHeader();

            // Validate that the serialized packet is large enough to contain at least the header and CRC.
            if (serializedPacket.Length < (headerSize + tailSize))
                throw new InvalidDataException($"Packet truncated or malformed; expected at least {headerSize + tailSize} bytes, got {serializedPacket.Length}.");

            // Create a span targeting the header portion of the serialized packet.
            ReadOnlySpan<byte> headerSpan = serializedPacket.Slice(0, headerSize);

            // Deserialize the header fields from the header span using BinaryPrimitives.ReadUIntx methods.
            newHeader.SessionId = BinaryPrimitives.ReadUInt64LittleEndian(headerSpan.Slice(0, 8));
            newHeader.BodyLength = BinaryPrimitives.ReadUInt32LittleEndian(headerSpan.Slice(8, 4));
            newHeader.MsgId = BinaryPrimitives.ReadUInt32LittleEndian(headerSpan.Slice(12, 4));
            newHeader.MsgType = BinaryPrimitives.ReadUInt16LittleEndian(headerSpan.Slice(16, 2));
            newHeader.BitFlags = BinaryPrimitives.ReadUInt16LittleEndian(headerSpan.Slice(18, 2));

            // Validate length now that we have deserialized it
            if (serializedPacket.Length < (headerSize + newHeader.BodyLength + tailSize))
                throw new InvalidDataException("Packet truncated or malformed. Packet body is smaller then expected.");

            // Create a span targeting the body.
            ReadOnlySpan<byte> bodySpan = serializedPacket.Slice(headerSize, (int)newHeader.BodyLength);

            // Create a span targeting the Crc value.
            ReadOnlySpan<byte> crcSpan = serializedPacket.Slice((headerSize + (int)newHeader.BodyLength), 4);

            // Copy the body span into a new byte array, since the body is stored as a byte array in the class.
            byte[] newBody = new byte[newHeader.BodyLength];
            bodySpan.CopyTo(newBody);

            // Read the Crc value from the crc span, and calculate the Crc value based on the header and body.
            UInt32 receivedCrC = BinaryPrimitives.ReadUInt32LittleEndian(crcSpan);
            UInt32 calculatedCrc = CalculateCrc(headerSpan.ToArray(), newBody);

            // Check if the Crc values match. 
            if(calculatedCrc != receivedCrC)
                throw new InvalidDataException($"CRC mismatch; Packet corrupted.");

            return new MuddyPacket(newHeader, newBody, receivedCrC);
        }
    }
}
