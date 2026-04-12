using Server.Core.CommandPipeline.ContextBuilder;
using Shared.Protocol.Transport;

namespace Server.Core.CommandPipeline.Policies
{
    public interface ISecondPassPolicy
    {
        Task<PolicyResult> CheckPolicyAsync(CommandContext context);
    }
}
