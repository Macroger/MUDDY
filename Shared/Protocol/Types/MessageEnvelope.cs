using Shared.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Protocol.Types
{
    public sealed class MessageEnvelope
    {
        public MessageId MessageId { get; init; }
        public SessionId SessionId { get; init; }

        public MessageType MessageType { get; init; }
        public MessageId? CorrelationId { get; init; }

        public int ProtocolVersion { get; init; }
        public MessageFlags Flags { get; init; }

        public DateTime TimestampUtc { get; init; }

        public ReadOnlyMemory<byte> Payload { get; init; }
    }
}
