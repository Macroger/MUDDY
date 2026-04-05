using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Network.Listener
{
    public interface IListenerErrorHandler
    {
        void OnListenerError(Exception exception);
    }

}
