using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Infrastructure.Lifecycle
{
    /// <summary>
    /// An interface used to mark items as stoppable, meaning they have a Stop method that can be called to stop their operation.
    /// This is typically used for components that need to perform cleanup or release resources when they are no longer needed.
    /// </summary>
    public interface IStoppable
    {
        void Stop();
    }
}
