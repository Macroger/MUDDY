using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Hashing.Crc32;

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
        private readonly byte[] body;
        private readonly UInt32 crc;

        private const int maxJSONBodySize = 16384; // Set max JSON body size to 16 KiloBytes.
        //private const int maxBinaryBlobSize = 2097152; // Set max binary blob size to 2 MegaBytes.

        public MuddyPacket(UInt32 msgId,
            UInt16 msgType,
            UInt16 bitFlags,
            byte[] body,
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
            newHeader.BodyLength = (UInt32) body.Length;    // Cast to UInt32 - since body length should never be negative, is safe.

      
            this.header = newHeader;
            this.body = body;

            // Calculate crc value based on the header and body.
            


        }

        private static void calculateCrC()
        {
            
        }



    }
}
