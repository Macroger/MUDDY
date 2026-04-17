namespace Shared.Identity
{
    /// <summary>
    /// A wrapper to represent the unique identifier for a client connection in the server.
    /// This helps prevent accidentally using a connection ID in place of another string value, and vice versa.
    /// </summary>
    public readonly struct ConnectionId : IEquatable<ConnectionId>
    {
        public string Value { get; }

        public ConnectionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("ConnectionId cannot be empty.", nameof(value));

            Value = value;
        }

        public bool Equals(ConnectionId other) => Value == other.Value;     /// Determines whether the specified ConnectionId is equal to the current ConnectionId.
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;     /// Determines whether the specified object is equal to the current ConnectionId.
        public override bool Equals(object? obj) => obj is ConnectionId other && Equals(other);         /// Determines whether the specified object is equal to the current ConnectionId.
        public static bool operator ==(ConnectionId left, ConnectionId right) => left.Equals(right);    /// Determines whether two ConnectionId instances are equal.
        public static bool operator !=(ConnectionId left, ConnectionId right) => !left.Equals(right);   /// Determines whether two ConnectionId instances are not equal.
    }
}
