using Shared.Identity;
using Shared.Protocol.Transport;

namespace Server.Core.Network.Worker
{
    public interface IConnectionWorker
    {
        public bool IsRunning { get; }
        public ConnectionId ConnId { get; }
        void Start();
        void Stop();
        bool SendMessage(TransportEnvelope msg);

        event EventHandler<TransportEnvelope> MessageReceived;
        event EventHandler ConnectionClosed;
        event EventHandler<Exception> ErrorOccurred;
    }
}
