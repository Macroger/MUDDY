using Server.Core.Network.Model;
using Shared.Protocol.Types;
using Shared.Identity;
using Shared.EventBus;

namespace Server.Core.Network.Supervisor
{
    public interface INetworkSupervisor
    {
        public bool IsListeningForConnections { get; }
        bool StartAcceptingClients();

        bool StopAcceptingClients();

        void CloseConnection(ConnectionId connectionId, ConnectionCloseReason reason);

        void ProcessNewConnection(AcceptedConnection acceptedConnection);

        void BroadcastMessage(Shared.Protocol.Types.ProtocolEnvelope msg);

        void SendToClient(ConnectionId client, Shared.Protocol.Types.ProtocolEnvelope msg);
    }
}
