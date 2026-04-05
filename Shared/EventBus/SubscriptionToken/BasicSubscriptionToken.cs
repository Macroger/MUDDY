using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.EventBus.SubscriptionToken
{
    internal sealed class BasicSubscriptionToken : ISubscriptionToken
    {
        private readonly BasicEventBus _eventBus;
        private readonly EventMessageType _eventType;
        private readonly Action<object> _handler;
        private bool _isDisposed;


        public BasicSubscriptionToken(
            BasicEventBus eventBus,
            EventMessageType eventType,
            Action<object> handler)
        {
            _eventBus = eventBus;
            _eventType = eventType;
            _handler = handler;
        }

        public void Dispose()
        {

            if (_isDisposed) return;

            _eventBus.Unsubscribe(_eventType, _handler);
            _isDisposed = true;

        }
    }
}
