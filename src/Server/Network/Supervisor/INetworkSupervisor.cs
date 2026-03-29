using Server.Network.Model;
using Shared.Protocol.Transport;
using Shared.Protocol.Types;
using Shared.Identity;

namespace Server.Network.Supervisor
{
    public interface INetworkSupervisor
    {
        bool CheckNetworkIsRunning();

        bool StartAcceptingClients();

        bool StopAcceptingClients();

        void CloseConnection(ConnectionId connectionId, ConnectionCloseReason reason);

        void ProcessNewConnection(AcceptedConnection acceptedConnection);

        bool BroadcastMessage(MessageEnvelope msg);

        void SendToClient(ConnectionId client, MessageEnvelope msg);
    }
}
