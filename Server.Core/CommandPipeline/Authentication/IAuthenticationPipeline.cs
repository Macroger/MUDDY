// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.Protocol.Transport;

namespace Server.Core.CommandPipeline.Authentication
{
    /// <summary>
    /// Pipeline for handling authentication commands.
    /// </summary>
    public interface IAuthenticationPipeline
    {
        /// <summary>
        /// Processes an authentication command (login or register).
        /// </summary>
        Task ProcessAuthCommandAsync(TransportEnvelope envelope);
    }
}
