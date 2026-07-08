using Client.Core.Infrastructure.Events;
using Client.Core.MessagePipeline.Handlers;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Network.Transport;
using Shared.Network.Types;

namespace Client.Core.MessagePipeline.Routers
{     
    public class BasicMessageRouter : IMessageRouter
    {
        private readonly Dictionary<PacketType, IMessageHandler> _handlers = new();
        private IEventBus _eventBus = null!;

        public BasicMessageRouter(IEventBus eventBus)
        {   
            _eventBus = eventBus;
        }

        /// <summary>
        /// Registers a handler for a specific message type or key.
        /// </summary>
        public bool RegisterHandler(PacketType key, IMessageHandler handler)
        {
            // Validate inputs
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_handlers.ContainsKey(key) == true) return false;

            try
            {
                // Add handler to dictionary
                _handlers.Add(key, handler);
            }
            catch (System.Exception ex)
            {
                _eventBus.Publish(
                    EventMessageType.CmdPipeline,
                    new MessagePipelineEvents.Errors.MessagePipelineError(ex.Message, ex)
                );
                return false;
            }

			return true;
        }

		public IMessageHandler? GetHandler(PacketEnvelope envelope)
		{
			if (envelope == null)
				throw new ArgumentNullException(nameof(envelope), "PacketEnvelope cannot be null");

			// Try to find a handler for this verb.
			_handlers.TryGetValue(envelope.MessageType, out IMessageHandler? handler);
			return handler;
		}

        public void Dispose()
        {
            foreach (var handler in _handlers.Values)
            {
                if (handler is IDisposable disposableHandler)
                {
                    disposableHandler.Dispose();
                }
            }
            _handlers.Clear();
        }
    }
}
