using Shared.Identity;
using Shared.Protocol.Types;
using System;

namespace Server.Network.Worker
{
    public interface IConnectionWorker
    {
        public bool IsRunning { get; }
        public ConnectionId ConnId { get; }
        void Start();
        void Stop();
        bool SendMessage(MessageEnvelope msg);

        event EventHandler<MessageEnvelope> MessageReceived;
        event EventHandler ConnectionClosed;
        event EventHandler<Exception> ErrorOccurred;
    }
}
