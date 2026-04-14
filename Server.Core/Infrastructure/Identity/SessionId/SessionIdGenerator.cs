namespace Server.Core.Infrastructure.Identity.SessionId
{
    public sealed class SessionIdGenerator : ISessionIdGenerator
    {
        public Shared.Identity.SessionId New()
        {
            // Generate a new GUID and normalize it to a compact string form.
            Guid value = Guid.NewGuid();

            return new Shared.Identity.SessionId(value);

        }
    }
}
