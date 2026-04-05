namespace Shared.Identity
{
    /// <summary>
    /// A wrapper to make it easier to see that we are dealing with a sessionID instead of just a normal ulong.
    /// It also helps prevent accidentally using a sessionID in place of another ulong value, and vice versa.
    /// </summary>
    public readonly struct SessionId
    {
        public String Value { get; }

        public SessionId(String value)
        {
            Value = value;
        }
    }
}