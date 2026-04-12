using Shared.Identity;

namespace Shared.Protocol.Messaging
{
    public class MessageEnvelope<T>
    {
        public MessageId MessageId { get; }
        public SessionId SessionId { get; }
        public T Payload { get; }

        public MessageEnvelope(
            MessageId messageId,
            SessionId sessionId,
            T payload)
        {
            MessageId = messageId;
            SessionId = sessionId;
            Payload = payload;
        }
    }
}
