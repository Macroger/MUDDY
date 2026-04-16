using Windows.Media.Audio;

namespace Shared.Protocol.Transport
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
        public const int Size = 28;

        public const int SessionIdSize = 16;
        public const int BodyLengthSize = 4;
        public const int MsgIdSize = 4;
        public const int MsgTypeSize = 2;
        public const int BitFlagsSize = 2;

        public const int SessionIdOffset = 0;                                  // SessionId is the first segment, so it goes into offset 0.
        public const int BodyLengthOffset = SessionIdOffset + SessionIdSize;  // BodyLength goes after SessionId, so its offset is the sum of SessionIdOffset and SesssionIdSize.
        public const int MsgIdOffset = BodyLengthOffset + BodyLengthSize;       // MsgId goes after BodyLength, so its offset is the sum of BodyLengthOffset and BodyLengthSize.
        public const int MsgTypeOffset = MsgIdOffset + MsgIdSize;               // MsgType goes after MsgId, so its offset is the sum of MsgIdOffset and MsgIdSize.
        public const int BitFlagsOffset = MsgTypeOffset + MsgTypeSize;          // BitFlags goes after MsgType, so its offset is the sum of MsgTypeOffset and MsgTypeSize. Total header size is the sum of all field sizes, which is 28 bytes.


        //public const int SessionIdOffset = 0;       // SessionId size is 16 bytes, so next field starts at offset 16.
        //public const int BodyLengthOffset = 16;     // BodyLength is 4 bytes, so next field starts at offset 20.
        //public const int MsgIdOffset = 20;          // MsgId is 4 bytes, so next field starts at offset 24.
        //public const int MsgTypeOffset = 24;        // MsgType is 2 bytes, so next field starts at offset 26.
        //public const int BitFlagsOffset = 26;       // BitFlags is 2 bytes, so total header size is 28 bytes.


        public Guid SessionId;
        public UInt32 BodyLength;
        public UInt32 MsgId;
        public UInt16 MsgType;
        public UInt16 BitFlags;
    }

    /// <summary>
    /// Represents a network packet containing a header, body, and CRC for integrity verification.
    /// </summary>
    /// <remarks>The packet can be constructed from binary data using MuddyPacketSerializer. 
    /// The class holds the deserialized packet data including header, body, and CRC for validation.
    /// Exceptions are thrown during deserialization if the packet is malformed or corrupted.</remarks>
    public sealed class MuddyPacket
    {        
        public MuddyPacketHeader Header { get; init; }          // Store the header as a struct for easy access to its fields.
        public byte[] Body { get; init; }                       // Store the body as a byte array, since it can be either JSON or binary data.

        public UInt32 Crc { get; init; }                            // Store the CRC value for integrity verification.

        public MuddyPacket(
            UInt32 bodyLength,
            UInt32 msgId,
            UInt16 msgType,
            UInt16 bitFlags,
            byte[] body,
            UInt32 crcValue = 0
            )
        {
            // Setup a new header object - to be filled in with incoming values.
            MuddyPacketHeader newHeader = new MuddyPacketHeader();

            // Assign values to the new header object.
            newHeader.MsgId = msgId;
            newHeader.MsgType = msgType;
            newHeader.BitFlags = bitFlags;
            newHeader.BodyLength = bodyLength;

            // Assign the new header and body to the class fields.
            this.Header = newHeader;
            this.Body = body.ToArray();     // Defensive copy - prevents external modifications to the body after the packet is constructed.
            this.Crc = crcValue;
        }

        /// <summary>
        /// Initializes a new instance of the MuddyPacket class with the specified header, body, and CRC value.
        /// </summary>
        /// <remarks>Use this constructor when you already have the header, body, and CRC values available, such as when
        /// deserializing a packet from a byte array.</remarks>
        /// <param name="header">The header information for the packet, containing metadata such as type and length.</param>
        /// <param name="body">The byte array representing the packet's body content. Cannot be null.</param>
        /// <param name="crc">The CRC32 checksum used to verify the integrity of the packet data.</param>
        public MuddyPacket(MuddyPacketHeader header, byte[] body, UInt32 crc = 0)
        {
            this.Header = header;
            this.Body = body;
            this.Crc = crc;
        }
    }
}
