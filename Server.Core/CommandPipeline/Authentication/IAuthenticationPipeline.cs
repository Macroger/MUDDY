using Shared.Protocol.Transport;
using System;
using System.Collections.Generic;
using System.Text;

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
