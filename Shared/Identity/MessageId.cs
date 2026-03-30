namespace Shared.Identity
{
    /// <summary>
    /// A wrapper to represent the unique identifier for a message type in the protocol.
    /// This helps prevent accidentally using a message ID in place of another uint value, and vice versa.
    /// </summary>
    public readonly struct MessageId
    {
        public uint Value { get; }

        public MessageId(uint value)
        {
            Value = value;
        }       
    }
}
