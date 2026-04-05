using Server.Core.Network.Worker;

namespace Server.Core.Network.Model
{
    public class ConnectionContext
    {
        public AcceptedConnection ClientConnection { get; init; }
        public IConnectionWorker Worker { get; init; }

        public CancellationTokenSource CancellationSource { get; }

        public CancellationToken Token => CancellationSource.Token;

        public ConnectionContext(AcceptedConnection acceptedConnection, IConnectionWorker worker, CancellationTokenSource cts)
        {
            ClientConnection = acceptedConnection;
            CancellationSource = cts;
            Worker = worker;
        }
    }
}
