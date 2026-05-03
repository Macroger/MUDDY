// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Shared.Identity
{
    /// <summary>
    /// A wrapper to represent the unique identifier for a message type in the protocol.
    /// This helps prevent accidentally using a message ID in place of another uint value, and vice versa.
    /// </summary>
    public readonly struct MessageId : IEquatable<MessageId>
    {
        public uint Value { get; }

        public MessageId(uint value)
        {
            Value = value;
        }

        public bool Equals(MessageId other) => Value == other.Value;        /// Determines whether the specified MessageId is equal to the current MessageId.
        public override int GetHashCode() => Value.GetHashCode();           /// Determines whether the specified object is equal to the current MessageId.
        public static bool operator ==(MessageId left, MessageId right) => left.Equals(right);  /// Determines whether two MessageId instances are equal.
        public static bool operator !=(MessageId left, MessageId right) => !left.Equals(right); /// Determines whether two MessageId instances are not equal.
        public override bool Equals(object? obj) => (obj is MessageId other && Equals(other));  /// Determines whether the specified object is equal to the current MessageId.
    }
}
