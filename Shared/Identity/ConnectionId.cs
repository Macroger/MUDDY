using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Identity
{
    /// <summary>
    /// Strongly-typed identifier representing a single accepted network connection.
    /// Value-based, immutable, and safe to use as a dictionary key.
    /// </summary>
    public readonly struct ConnectionId : IEquatable<ConnectionId>
    {
        // Underlying value used for identity and logging.
        // Kept private to enforce invariants via the constructor.
        private readonly string _value;

        /// <summary>
        /// Creates a new ConnectionId with enforced validity rules.
        /// Empty or whitespace-only values are not allowed.
        /// </summary>
        public ConnectionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("ConnectionId cannot be empty.", nameof(value));

            _value = value;
        }

        /// <summary>
        /// Returns the underlying value for logging and diagnostics.
        /// </summary>
        public override string ToString() => _value;

        /// <summary>
        /// Value-based equality comparison.
        /// Two ConnectionIds are equal if their underlying values match.
        /// </summary>
        public bool Equals(ConnectionId other) => _value == other._value;

        /// <summary>
        /// Object-based equality required by .NET.
        /// Delegates to the strongly-typed Equals method.
        /// </summary>
        public override bool Equals(object? obj)
            => obj is ConnectionId other && Equals(other);

        /// <summary>
        /// Hash code derived from the underlying value.
        /// Required for correct behavior in hash-based collections
        /// (e.g., Dictionary, HashSet).
        /// </summary>
        public override int GetHashCode() => _value.GetHashCode();

        /// <summary>
        /// Equality operators for clean, intent-revealing comparisons.
        /// </summary>
        public static bool operator ==(ConnectionId left, ConnectionId right)
            => left.Equals(right);

        public static bool operator !=(ConnectionId left, ConnectionId right)
            => !left.Equals(right);
    }
}
