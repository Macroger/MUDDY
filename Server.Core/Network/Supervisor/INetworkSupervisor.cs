using Server.Core.Network.Model;
using Shared.Protocol.Types;
using Shared.Identity;
using Shared.EventBus;
using Shared.Protocol.Transport;

namespace Server.Core.Network.Supervisor
{
    public interface INetworkSupervisor
    {
        public bool IsListeningForConnections { get; }
        bool StartAcceptingClients();

        bool StopAcceptingClients();

        void CloseConnection(ConnectionId connectionId, ConnectionCloseReason reason);

        void ProcessNewConnection(AcceptedConnection acceptedConnection);

        void BroadcastMessage(TransportEnvelope msg);

        void SendToClient(ConnectionId client, TransportEnvelope msg);
    }
}
