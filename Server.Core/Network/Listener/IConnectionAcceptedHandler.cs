using Server.Core.Network.Model;

namespace Server.Core.Network.Listener
{
    public interface IConnectionAcceptedHandler
    {
        void OnConnectionAccepted(AcceptedConnection connection);
    }
}
