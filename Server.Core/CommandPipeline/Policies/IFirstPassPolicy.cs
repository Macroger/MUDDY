using Shared.Protocol.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Security.EnterpriseData;

namespace Server.Core.CommandPipeline.Policies
{
    public interface IFirstPassPolicy
    {
        PolicyResult CheckPolicy(TransportEnvelope msg);


    }
}
