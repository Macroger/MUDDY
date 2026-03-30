using Shared.Identity;

namespace Shared.Protocol.Types
{
    public sealed class MessageEnvelope
    {
        public MessageId MessageId { get; init; }
        //public SessionId SessionId { get; init; }

        public MessageType MessageType { get; init; }
        //public MessageId? CorrelationId { get; init; }

        //public int ProtocolVersion { get; init; }
        public MessageFlags Flags { get; init; }

        public DateTime TimestampUtc { get; init; }

        public byte[] Payload { get; init; }

        public MessageEnvelope(
            MessageId messageId,
            MessageType messageType,            
            MessageFlags flags,
            byte[] payload
            )
        {
            MessageId = messageId;
            MessageType = messageType;
            Flags = flags;
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            TimestampUtc = DateTime.UtcNow;
        }
    }
}
