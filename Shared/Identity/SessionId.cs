using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Identity
{
    public readonly struct SessionId : IEquatable<SessionId>
    {
        private readonly string _value;

        public SessionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("SessionId cannot be empty.", nameof(value));

            _value = value;
        }

        public override string ToString() => _value;

        public bool Equals(SessionId other) => _value == other._value;
        public override bool Equals(object? obj) => obj is SessionId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(SessionId left, SessionId right) => left.Equals(right);
        public static bool operator !=(SessionId left, SessionId right) => !left.Equals(right);
    }
}
