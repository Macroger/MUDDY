using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Identity
{
    public readonly struct MessageId : IEquatable<MessageId>
    {
        private readonly string _value;

        public MessageId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("MessageId cannot be empty.", nameof(value));

            _value = value;
        }

        public override string ToString() => _value;

        public bool Equals(MessageId other) => _value == other._value;
        public override bool Equals(object? obj) => obj is MessageId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(MessageId left, MessageId right) => left.Equals(right);
        public static bool operator !=(MessageId left, MessageId right) => !left.Equals(right);
    }
}
