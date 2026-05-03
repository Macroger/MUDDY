// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Shared.EventBus
{
    /// <summary>
    /// A container for an event message, including its type, payload, and metadata such as creation time and a unique identifier.
    /// </summary>
    public sealed class EventEnvelope
    {
        public EventEnvelope(
            EventMessageType messageType,
            object payload)
        {
            MsgType = messageType;
            Payload = payload;
            EnvelopeId = Guid.NewGuid();
            CreatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the type of the message represented by this instance.
        /// </summary>
        public EventMessageType MsgType { get; }

        /// <summary>
        /// Gets the payload associated with this instance.
        /// </summary>
        public object Payload { get; }

        /// <summary>
        /// Gets the unique identifier for the envelope.
        /// </summary>
        public Guid EnvelopeId { get; }

        /// <summary>
        /// Gets the date and time when the object was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; }
        // When the envelope was created and published
    }
}
