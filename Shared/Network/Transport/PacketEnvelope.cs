/**
 * @file PacketEnvelope.cs
 * @namespace Shared.Network.Transport
 * @brief Encapsulates a network message payload with transport metadata.
 * @details Represents an in-memory envelope used by client/server pipeline stages.
 *          Includes optional message, connection, and session identities, message
 *          type/flags, payload bytes, and creation timestamp.
 */

using Shared.Identity;
using Shared.Network.Types;

namespace Shared.Network.Transport
{
    /// <summary>
    /// Encapsulates a protocol message with metadata and payload. 
    /// It envelopes a message coming across the wire and adds metadata such as the message type, flags, and timestamp.
    /// </summary>
    public sealed class PacketEnvelope
    {
        /// <summary>Unique identifier for the message.</summary>
        public MessageId? MessageId { get; init; }

        // The connection ID of the worker that this message is associated with.
        public ConnectionId? ConnId { get; init; }

        /// <summary>
        /// Optional session token associated with the message. 
        /// This can be used to track the session context for messages that are part of a session-based communication.
        /// </summary>
        public SessionId? SessionToken { get; init; }

        /// <summary>The message type based on PacketType.</summary>
        public PacketType MessageType { get; init; }

        /// <summary>Flags associated with the message.</summary>
        public MessageFlags? Flags { get; init; }

        /// <summary>UTC timestamp when the envelope was created.</summary>
        public DateTime TimestampUtc { get; init; }

        /// <summary>Binary payload carried by the envelope.</summary>
        public byte[] Payload { get; init; }

        /// <summary>
        /// Construct a new PacketEnvelope instance.
        /// </summary>
        /// <param name="messageId">Unique message identifier.</param>
        /// <param name="messageType">The protocol message type. Must not be null.</param>
        /// <param name="flags">Message flags.</param>
        /// <param name="payload">Binary payload for the message. Must not be null.</param>
        /// <param name="connectionId">Optional connection ID associated with the message.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="payload"/> is null.</exception>
        public PacketEnvelope(            
            PacketType messageType,            
            byte[] payload,
            MessageId? messageId = null,
            MessageFlags? flags = null,
            ConnectionId? connectionId = null,
            SessionId? sessionId = null,
            MessageId? messageCorrelationId = null
            )
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            MessageId = messageId;
            SessionToken = sessionId;
            MessageType = messageType;
            Flags = flags;
            Payload = payload;
            ConnId = connectionId;
            TimestampUtc = DateTime.UtcNow;
        }
    }
}
