using Server.Core.Command_Pipeline;
using Server.Core.Network.Supervisor;
using Shared.EventBus;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Application
{
    public class SystemInitializer
    {
        private readonly IEventBus _eventBus;
        private readonly INetworkSupervisor _networkSupervisor;
        private readonly CommandPipelineOrchestrator _commandPipelineOrchestrator;
    }
}
