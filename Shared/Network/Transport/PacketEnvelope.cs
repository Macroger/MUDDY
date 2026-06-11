// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
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
        public MessageId MessageId { get; init; }

        // The connection ID of the worker that this message is associated with, if applicable.
        public ConnectionId ConnId { get; init; }

        public SessionId? SessionToken { get; init; }

        /// <summary>
        /// Gets the identifier used to correlate this message with related messages.
        /// </summary>
        /// <remarks>Use this property to associate this message with a specific request, response, or
        /// conversation. The correlation identifier enables tracking and grouping of related messages across
        /// distributed systems.</remarks>
        public MessageId? CorrelationId { get; init; }

        /// <summary>The message type based on PacketType.</summary>
        public PacketType MessageType { get; init; }

        /// <summary>Flags associated with the message.</summary>
        public MessageFlags Flags { get; init; }

        /// <summary>UTC timestamp when the envelope was created.</summary>
        public DateTime TimestampUtc { get; init; }

        /// <summary>Binary payload carried by the envelope.</summary>
        public byte[] Payload { get; init; }

        /// <summary>
        /// Construct a new PacketEnvelope instance.
        /// </summary>
        /// <param name="messageId">Unique message identifier.</param>
        /// <param name="messageType">The protocol message type.</param>
        /// <param name="flags">Message flags.</param>
        /// <param name="payload">Binary payload for the message. Must not be null.</param>
        /// <param name="connectionId">Optional connection ID associated with the message.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="payload"/> is null.</exception>
        public PacketEnvelope(
            MessageId messageId,
            PacketType messageType,
            MessageFlags flags,
            byte[] payload,
            ConnectionId connectionId,
            SessionId? sessionId,
            MessageId? messageCorrelationId = null
            )
        {
            MessageId = messageId;
            SessionToken = sessionId;
            MessageType = messageType;
            Flags = flags;
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            ConnId = connectionId;
            CorrelationId = messageCorrelationId;
            TimestampUtc = DateTime.UtcNow;
        }
    }
}
