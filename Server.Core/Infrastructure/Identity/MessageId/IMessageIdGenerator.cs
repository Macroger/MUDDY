namespace Server.Core.Infrastructure.Identity.MessageId
{
    public interface IMessageIdGenerator
    {
        Shared.Identity.MessageId New();
    }
}
