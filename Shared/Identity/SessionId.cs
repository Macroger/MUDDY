// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
namespace Shared.Identity
{
    /// <summary>
    /// A wrapper to make it easier to see that we are dealing with a sessionID instead of just a normal ulong.
    /// It also helps prevent accidentally using a sessionID in place of another ulong value, and vice versa.
    /// </summary>
    public readonly struct SessionId : IEquatable<SessionId>
    {
        public Guid Value { get; }

        public SessionId(Guid value)
        {
            Value = value;
        }

        public static SessionId Unauthenticated => new SessionId(Guid.Empty);   /// Returns a SessionId representing an unauthenticated session.
        public bool Equals(SessionId other) => Value == other.Value;            /// Determines whether the specified SessionId is equal to the current SessionId.
        public override int GetHashCode() => Value.GetHashCode();               /// Returns a hash code for the current SessionId.
        public static bool operator ==(SessionId left, SessionId right) => left.Equals(right); /// Determines whether two SessionId instances are equal.
        public static bool operator !=(SessionId left, SessionId right) => !(left == right);   /// Determines whether two SessionId instances are not equal.
        public override bool Equals(object? obj) => obj is SessionId other && Equals(other);   /// Determines whether the specified object is equal to the current SessionId.
    }
}