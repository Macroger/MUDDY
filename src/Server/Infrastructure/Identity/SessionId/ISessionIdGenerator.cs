using Shared.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Infrastructure.Identity.SessionId
{
    public interface ISessionIdGenerator
    {
        Shared.Identity.SessionId New();
    }

}
