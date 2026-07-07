// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.Identity;
using Shared.Network.Transport;

namespace Server.Core.Network.Worker
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
