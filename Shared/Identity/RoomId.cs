using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Identity
{
    /// <summary>
    /// A wrapper to represent the unique identifier for a room in the game world.
    /// This helps prevent accidentally using a room ID in place of another string value, and vice versa.
    /// </summary>
    public readonly struct RoomId : IEquatable<RoomId>
    {
        public string Value { get; }

        public RoomId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("RoomId cannot be empty.", nameof(value));

            Value = value;
        }

        public bool Equals(RoomId other) => Value == other.Value;       /// Determines whether the specified RoomId is equal to the current RoomId.       
        public override int GetHashCode() => Value.GetHashCode();       /// Returns a hash code for the current RoomId.
        public override bool Equals(object? obj) => obj is RoomId other && Equals(other);   /// Determines whether the specified object is equal to the current RoomId.
        public static bool operator ==(RoomId left, RoomId right) => left.Equals(right);    /// Determines whether two RoomId instances are equal.
        public static bool operator !=(RoomId left, RoomId right) => !left.Equals(right);   /// Determines whether two RoomId instances are not equal.
    }
}
