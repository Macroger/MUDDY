using Shared.Identity;
using System;
using System.Collections.Generic;
using System.Text;


namespace Server.Infrastructure.Identity.MessageId
{
    public interface IMessageIdGenerator
    {
        Shared.Identity.MessageId New();
    }
}
