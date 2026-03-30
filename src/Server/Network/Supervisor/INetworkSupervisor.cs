using Server.Network.Model;
using Shared.Protocol.Types;
using Shared.Identity;

namespace Server.Network.Supervisor
{
    public interface INetworkSupervisor
    {
        public bool IsListeningForConnections { get; }
        bool StartAcceptingClients();

        bool StopAcceptingClients();

        void CloseConnection(ConnectionId connectionId, ConnectionCloseReason reason);

        void ProcessNewConnection(AcceptedConnection acceptedConnection);

        void BroadcastMessage(MessageEnvelope msg);

        void SendToClient(ConnectionId client, MessageEnvelope msg);
    }
}
