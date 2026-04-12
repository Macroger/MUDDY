using Server.Core.CommandPipeline.ContextBuilder;
using Shared.Protocol.Transport;

namespace Server.Core.CommandPipeline.Policies
{
    public interface ISecondPassPolicy
    {
        PolicyResult CheckPolicy(CommandContext context);
    }
}
