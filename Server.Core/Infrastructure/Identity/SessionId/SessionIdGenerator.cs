namespace Server.Core.Infrastructure.Identity.SessionId
{
    public sealed class SessionIdGenerator : ISessionIdGenerator
    {
        public Shared.Identity.SessionId New()
        {
            // Generate a new GUID and normalize it to a compact string form.
            string value = Guid.NewGuid().ToString("N");

            return new Shared.Identity.SessionId(value);

        }
    }
}
