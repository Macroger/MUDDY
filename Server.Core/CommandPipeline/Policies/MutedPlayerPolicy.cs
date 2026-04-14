using Server.Core.CommandPipeline.ContextBuilder;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.CommandPipeline.Policies
{
    /// <summary>
    /// Enforces that muted players cannot send chat messages.
    /// </summary>
    public sealed class MutedPlayerPolicy : ISecondPassPolicy
    {
        public Task<PolicyResult> CheckPolicyAsync(CommandContext context)
        {
            // Check if player has Muted condition
            if (context.PlayerState?.ActiveConditions.Contains(PlayerCondition.Muted) ?? false)
            {
                // Check if command is a "say" command
                if (context.Command?.CommandType.Equals("say", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    return Task.FromResult(new PolicyResult
                    {
                        IsValid = false,
                        ErrorMessage = "You are muted and cannot speak."
                    });
                }
            }

            return Task.FromResult(PolicyResult.Success());
        }
    }
}
