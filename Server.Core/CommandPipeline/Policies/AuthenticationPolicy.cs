using Shared.Protocol.Transport;

namespace Server.Core.CommandPipeline.Policies
{
    /// <summary>
    /// First-pass policy that validates session authentication.
    /// If SessionId is 0 (unauthenticated), only allows login/register commands.
    /// If SessionId is non-zero, validates it's a real session.
    /// </summary>
    public class AuthenticationPolicy : IFirstPassPolicy
    {
        public PolicyResult CheckPolicy(TransportEnvelope msg)
        {
            throw new NotImplementedException();
        }
    }
}
