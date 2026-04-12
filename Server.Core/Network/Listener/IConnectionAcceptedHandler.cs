using Server.Core.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Network.Listener
{
    public interface IConnectionAcceptedHandler
    {
        void OnConnectionAccepted(AcceptedConnection connection);
    }
}
