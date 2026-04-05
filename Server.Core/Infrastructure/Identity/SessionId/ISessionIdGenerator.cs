namespace Server.Core.Infrastructure.Identity.SessionId
{
    public interface ISessionIdGenerator
    {
        Shared.Identity.SessionId New();
    }

}
