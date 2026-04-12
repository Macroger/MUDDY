using Shared.Protocol.Transport;

namespace Server.Core.CommandPipeline.Policies
{
    public interface IFirstPassPolicy
    {
        Task<PolicyResult> CheckPolicyAsync(TransportEnvelope msg);
    }
}
