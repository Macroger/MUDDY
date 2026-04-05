using Shared.Identity;
using Shared.Protocol.Types;

namespace Server.Core.Network.Worker
{
    public interface IConnectionWorker
    {
        public bool IsRunning { get; }
        public ConnectionId ConnId { get; }
        void Start();
        void Stop();
        bool SendMessage(Shared.Protocol.Types.ProtocolEnvelope msg);

        event EventHandler<Shared.Protocol.Types.ProtocolEnvelope> MessageReceived;
        event EventHandler ConnectionClosed;
        event EventHandler<Exception> ErrorOccurred;
    }
}
