namespace Shared.Identity
{
    /// <summary>
    /// A wrapper to represent the unique identifier for a client connection in the server.
    /// This helps prevent accidentally using a connection ID in place of another string value, and vice versa.
    /// </summary>
    public readonly struct ConnectionId
    {
        private readonly string _value;

        public ConnectionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("ConnectionId cannot be empty.", nameof(value));

            _value = value;
        }
    }
}
