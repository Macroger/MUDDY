using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Application
{
    public interface IServerCoreLifecycle
    {
        void StartServer();
        void ShutdownServer();
    }
}
