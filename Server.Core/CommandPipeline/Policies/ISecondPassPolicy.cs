// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.CommandPipeline.ContextBuilder;

namespace Server.Core.CommandPipeline.Policies
{
    public interface ISecondPassPolicy
    {
        Task<PolicyResult> CheckPolicyAsync(CommandContext context);
    }
}
