using Shared.Identity;

namespace Shared.Protocol.Types
{
    public sealed class ProtocolEnvelope
    {
        public MessageId MessageId { get; init; }
        //public SessionId SessionId { get; init; }

        public ProtocolMessageType MessageType { get; init; }
        //public MessageId? CorrelationId { get; init; }

        //public int ProtocolVersion { get; init; }
        public MessageFlags Flags { get; init; }

        public DateTime TimestampUtc { get; init; }

        public byte[] Payload { get; init; }

        public ProtocolEnvelope(
            MessageId messageId,
            ProtocolMessageType messageType,            
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
