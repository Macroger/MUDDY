using System.IO.Hashing;
using System.Buffers.Binary;
using System.Text;

namespace Shared.Networking
{
    /// <summary>
    /// A struct representing the header segment of a MuddyPacket.
    /// </summary>
    public struct MuddyPacketHeader
    {
        public UInt64 SessionId;
        public UInt32 BodyLength;
        public UInt32 MsgId;
        public UInt16 MsgType;
        public UInt16 BitFlags;
    }

    public class MuddyPacket
    {
        private readonly MuddyPacketHeader header;
        private readonly byte[] serializedHeader;
        private readonly byte[] serializedBody;
        private readonly UInt32 crc;

        private const int headerSize = 20; // The size in bytes of the header.
        private const int maxJSONBodySize = 16384; // Set max JSON body size to 16 KiloBytes.
        //private const int maxBinaryBlobSize = 2097152; // Set max binary blob size to 2 MegaBytes.

        public MuddyPacket(UInt32 msgId,
            UInt16 msgType,
            UInt16 bitFlags,
            String body,
            UInt64 sessionId = 0)
        {
            // First, check if body size is acceptable.
            // Assume JSON packet for now - BinaryBlobs to be added later, will require checks bitFlag.
            if (body.Length < maxJSONBodySize)
                throw new ArgumentException($"Body size exceeds maximum allowed of {maxJSONBodySize} bytes.");

            // Setup a new header object - to be filled in with incoming values.
            MuddyPacketHeader newHeader = new MuddyPacketHeader();

            // Assign values to the new header object.
            newHeader.SessionId = sessionId;
            newHeader.MsgId = msgId;
            newHeader.MsgType = msgType;
            newHeader.BitFlags = bitFlags;
            newHeader.BodyLength = (UInt32) body.Length;    // Cast to UInt32 - since body length should never be negative, is safe
      
            header = newHeader;
            serializedHeader = SerializeHeader(newHeader);
            serializedBody = Encoding.UTF8.GetBytes(body);

            // Calculate crc value based on the header and body.
            crc = CalculateCrc(serializedHeader, serializedBody);
        }

        public MuddyPacket(MuddyPacketHeader header, byte[] body, UInt32 crc)
        {
            this.header = header;
            this.serializedHeader = SerializeHeader(header);
            this.serializedBody = body;
            this.crc = crc;
        }
        private static UInt32 CalculateCrc(byte[] header, byte[] body)
        {
            Crc32 crc = new();

            crc.Append(header);
            crc.Append(body);

            return crc.GetCurrentHashAsUInt32();
        }

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

        public byte[] Serialize()
        {
            // Create a set of variables to hold the packet segment sizes (mostly for convenience).
            int bodySize = (int)header.BodyLength;
            const int tailSize = sizeof(UInt32);
           
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

        public static MuddyPacket Deserialize(ReadOnlySpan<byte> serializedPacket)
        {
            MuddyPacketHeader newHeader = new MuddyPacketHeader();

            ReadOnlySpan<byte> headerSpan = serializedPacket.Slice(0, headerSize);

            newHeader.SessionId = BinaryPrimitives.ReadUInt64LittleEndian(headerSpan.Slice(0, 8));
            newHeader.BodyLength = BinaryPrimitives.ReadUInt32LittleEndian(headerSpan.Slice(8, 4));
            newHeader.MsgId = BinaryPrimitives.ReadUInt32LittleEndian(headerSpan.Slice(12, 4));
            newHeader.MsgType = BinaryPrimitives.ReadUInt16LittleEndian(headerSpan.Slice(16, 2));
            newHeader.BitFlags = BinaryPrimitives.ReadUInt16LittleEndian(headerSpan.Slice(18, 2));

            // Validate length now that we have deserialized it
            if (serializedPacket.Length < newHeader.BodyLength)
                throw new InvalidDataException("Packet truncated or malformed.");

            // Create a span targeting the body.
            ReadOnlySpan<byte> bodySpan = serializedPacket.Slice(headerSize, (int)newHeader.BodyLength);

            // Create a span targeting the Crc value.
            ReadOnlySpan<byte> crcSpan = serializedPacket.Slice((headerSize + (int)newHeader.BodyLength), 4);

            byte[] newBody = new byte[newHeader.BodyLength];
            bodySpan.CopyTo(newBody);

            UInt32 receivedCrC = BinaryPrimitives.ReadUInt32LittleEndian(crcSpan);
            UInt32 calculatedCrc = CalculateCrc(headerSpan.ToArray(), newBody);

            // Check if the Crc values match. 
            if(calculatedCrc != receivedCrC)
                throw new InvalidDataException($"CRC mismatch; Packet corrupted.");

            return new MuddyPacket(newHeader, newBody, receivedCrC);
        }
    }
}
