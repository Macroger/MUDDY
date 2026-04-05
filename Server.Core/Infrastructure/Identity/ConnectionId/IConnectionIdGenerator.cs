namespace Server.Core.Infrastructure.Identity.ConnectionId
{
    public interface IConnectionIdGenerator
    {
        Shared.Identity.ConnectionId New();
    }
}
