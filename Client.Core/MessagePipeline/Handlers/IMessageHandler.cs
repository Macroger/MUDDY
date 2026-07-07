//using Server.Core.CommandPipeline.ContextBuilder;
//using Server.Core.CommandPipeline.Types;

using Shared.Network.Transport;
using Shared.Network.Types;

namespace Client.Core.MessagePipeline.Handlers
{
    /// <summary>
    /// Defines the contract for command handlers that execute parsed commands.
    /// </summary>
    public interface IMessageHandler
    {
        public PacketType MessageType { get; init; }

        /// <summary>
        /// Executes the command with the given context and returns the result.
        /// </summary>
        /// <param name="envelope">The transport envelope containing the message and metadata.</param>
        /// <returns>A task representing the command execution result.</returns>
        Task ExecuteAsync(MessageEnvelope envelope);
    }
}
