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

        /// <summary>
        /// Returns a SessionId representing an unauthenticated session.
        /// </summary>
        public static SessionId Unauthenticated => new SessionId(Guid.Empty);

        /// <summary>Compares two SessionId values for equality.</summary>
        public bool Equals(SessionId other) => Value == other.Value;

        /// <summary>Returns the hash code for this SessionId.</summary>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>Determines whether two SessionId values are equal.</summary>
        public static bool operator ==(SessionId left, SessionId right) => left.Equals(right);

        /// <summary>Determines whether two SessionId values are not equal.</summary>
        public static bool operator !=(SessionId left, SessionId right) => !(left == right);

        /// <summary>Compares this SessionId with an object for equality.</summary>
        public override bool Equals(object? obj) => obj is SessionId other && Equals(other);
    }
}