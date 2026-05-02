// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.Protocol.Transport;

namespace Server.Core.CommandPipeline.Policies
{
    public interface IFirstPassPolicy
    {
        Task<PolicyResult> CheckPolicyAsync(TransportEnvelope msg);
    }
}
