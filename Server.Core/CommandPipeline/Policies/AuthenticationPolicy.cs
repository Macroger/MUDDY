// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.Domain.Authentication;
using Shared.Identity;
using Shared.Network.Transport;

namespace Server.Core.CommandPipeline.Policies
{
    /// <summary>
    /// First-pass policy that validates session authentication.
    /// If SessionId is 0 (unauthenticated), only allows login/register commands.
    /// If SessionId is non-zero, validates it's a real session.
    /// </summary>
    public class AuthenticationPolicy : IFirstPassPolicy
    {
        private readonly IAuthenticationService _authService;

        /// <summary>
        /// Creates a new AuthenticationPolicy with the given auth service.
        /// </summary>
        public AuthenticationPolicy(IAuthenticationService authService)
        {
            _authService = authService;
        }
        public async Task<PolicyResult> CheckPolicyAsync(PacketEnvelope msg)
        {
            // If unauthenticated (SessionId = 0), allow it to pass
            // The orchestrator will route to auth pipeline
            if (msg.SessionToken == SessionId.Unauthenticated)
            {
                return await Task.FromResult(PolicyResult.Success());
            }

            // If authenticated, validate the session is real
            bool isValid = await _authService.ValidateSessionAsync(msg.SessionToken, msg.ConnId);

            if (!isValid)
            {
                // Session is no longer valid, reject the message. The client will need to re-authenticate.
                return await Task.FromResult(PolicyResult.Failure("Invalid or expired session. Please reconnect to get a new session."));
            }

            return await Task.FromResult(PolicyResult.Success());
        }
    }
}
