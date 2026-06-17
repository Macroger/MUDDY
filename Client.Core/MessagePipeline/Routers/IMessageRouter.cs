using Shared.Network.Transport;
using Shared.Network.Types;

namespace Client.Core.MessagePipeline.Routers
{
    public interface IMessageRouter: IDisposable
    {
        /// <summary>
        /// Registers a handler for a specific command verb.
        /// </summary>
        /// <param name="verb">The command verb.</param>
        /// <param name="handler">The handler to execute for this verb.</param>
        /// <returns>True if the handler was registered; false if a handler already exists for this verb.</returns>
        bool RegisterHandler(PacketType msgType, Handlers.IMessageHandler handler);

        /// <summary>
        /// Routes a parsed command to its corresponding handler.
        /// </summary>
        /// <param name="envelope">The envelope containing the command with verb and arguments.</param>
        /// <returns>The matching command handler, or null if the command is unknown.</returns>
        Handlers.IMessageHandler? GetHandler(PacketEnvelope envelope);
    }
}
