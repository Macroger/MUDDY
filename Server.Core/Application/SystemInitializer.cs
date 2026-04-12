using Server.Core.CommandPipeline;
using Server.Core.Infrastructure.Lifecycle;
using Server.Core.Network.Listener;
using Server.Core.Network.Supervisor;
using Shared.EventBus;

namespace Server.Core.Application
{
    public class SystemInitializer
    {
        private readonly IEventBus _eventBus;
        private readonly INetworkSupervisor _networkSupervisor;
        private readonly CommandPipelineOrchestrator _commandPipelineOrchestrator;
        private readonly LifecycleCoordinator _lifecycleCoordinator;

        public SystemInitializer(IEventBus eventBus)
        {
            _eventBus = eventBus;
            _lifecycleCoordinator = new LifecycleCoordinator();

            // Create the command pipeline orchestrator
            _commandPipelineOrchestrator = new CommandPipelineOrchestrator();
           
            // Create the network supervisor
            _networkSupervisor = new StandardNetworkSupervisor(
                _commandPipelineOrchestrator,   // Provide a reference to the command pipeline orchestrator.
                _lifecycleCoordinator,          // Provide reference to the lifecycle coordinator for state-aware behavior.
                _eventBus                       // Provide a reference to the eventBus.
            );

            // Register components for managed lifecycle
            RegisterLifecycleComponent(_networkSupervisor);
        }

        /// <summary>
        /// Registers a component with the lifecycle coordinator for all lifecycle interfaces it implements.
        /// </summary>
        /// <param name="component">The component to register. Can implement IStartable, IStoppable, and/or IShutdownAware.</param>
        private void RegisterLifecycleComponent(object component)
        {
            if (component is IStartable startable)
                _lifecycleCoordinator.RegisterStartableItem(startable);

            if (component is IStoppable stoppable)
                _lifecycleCoordinator.RegisterStoppableItem(stoppable);

            if (component is IShutdownAware shutdownAware)
                _lifecycleCoordinator.RegisterShutdownAwareItem(shutdownAware);
        }

        public void StartServer()
        {
            // Start the lifecycle coordinator, which will in turn start all registered startable items.
            _lifecycleCoordinator.StartServer();
        }
    }
}
