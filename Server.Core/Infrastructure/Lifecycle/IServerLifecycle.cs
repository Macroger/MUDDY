using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Infrastructure.Lifecycle
{

    /// <summary>
    /// An interface to mark items as server lifecycle components, meaning they have a
    /// Shutdown method that can be called to perform any necessary cleanup or finalization before the server is shut down.  
    /// </summary>
    public interface IServerLifecycle
    {
        bool IsLoading { get; }
        bool IsActive { get; }
        bool IsShuttingDown { get; }
        bool IsInMaintenance { get; }

        bool StartServer();
        bool ShutdownServer();
        bool SetState(ServerStateEnum newState);
        bool TryTransition(ServerStateEnum expected, ServerStateEnum next);
    }

}
