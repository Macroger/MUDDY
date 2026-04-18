using Server.Core.CommandPipeline.ContextBuilder;

namespace Server.Core.CommandPipeline.Policies
{
    public interface ISecondPassPolicy
    {
        Task<PolicyResult> CheckPolicyAsync(CommandContext context);
    }
}
