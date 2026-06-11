using Client.Core.MessagePipeline.Routers;
using Shared.Network.Types;

namespace Client.Core.MessagePipeline.Registrators

{
    public interface IMessageRegistrator
    {
        void Register(IMessageRouter router);
    }
}

