using Server.Core.CommandPipeline;
using Server.Core.CommandPipeline.Authentication;
using Server.Core.CommandPipeline.CommandHandler;
using Server.Core.CommandPipeline.CommandRegistration;
using Server.Core.CommandPipeline.CommandRouter;
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Parser;
using Server.Core.CommandPipeline.Policies;
using Server.Core.Domain.Authentication;
using Server.Core.Domain.Services.ChatService;
using Server.Core.Domain.Services.PlayerQueryService;
using Server.Core.Domain.Services.WorldMovementService;
using Server.Core.Domain.Services.WorldQueryService;
using Server.Core.Infrastructure.Events;
using Server.Core.Infrastructure.Identity.MessageId;
using Server.Core.Infrastructure.Identity.SessionId;
using Server.Core.Infrastructure.Lifecycle;
using Server.Core.Network.Supervisor;
using Server.Core.Persistence;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Logging;

namespace Server.Core.Application
{
    public class SystemInitializer
    {
        private readonly CommandPipelineOrchestrator _commandPipelineOrchestrator;
        private readonly LifecycleCoordinator _lifecycleCoordinator;

        private readonly IEventBus _eventBus;
        private readonly INetworkSupervisor _networkSupervisor;

        private readonly IMessageIdGenerator _messageIdGenerator;
        private readonly ISessionIdGenerator _sessionIdGenerator;

        private readonly IPlayerRepository _playerRepository;
        private readonly IWorldRepository _worldRepository;

        private readonly IChatService _chatService;
        private readonly IWorldMovementService _movementService;
        private readonly IWorldQueryService _worldQueryService;
        private readonly IPlayerQueryService _playerQueryService;

        public SystemInitializer()
        {
            try
            {
                _eventBus = new BasicEventBus();

                _messageIdGenerator = new MessageIdGenerator();
                _sessionIdGenerator = new SessionIdGenerator();

                _lifecycleCoordinator = new LifecycleCoordinator(_eventBus);

                _playerRepository = new InMemoryPlayerRepository(_eventBus);
                _worldRepository = new InMemoryWorldRepository(_eventBus);

                // Create a file path for the logger to use, using the LogPathHelper to ensure it's in a safe, writable location.
                string logFilePath = Shared.Logging.LogPathHelper.CreateTimestampedLogPath("server_packets");

                // Create the file logger and subscribe it to the event bus.
                FileLogger _fileLogger = new FileLogger(
                    _eventBus,      // Provide the event bus for subscribing to events.
                    LogLevel.Debug, // Set minimum log level to Debug to capture all events. Adjust as needed.
                    logFilePath     // Use the LogPathHelper to get a safe path for the log file.
                );      

                // Create the network supervisor
                _networkSupervisor = new StandardNetworkSupervisor(
                    _lifecycleCoordinator,          // Provide reference to the lifecycle coordinator for state-aware behavior.
                    _eventBus,                      // Provide a reference to the eventBus.
                    _messageIdGenerator             // Provide a reference to the messageIdGenerator
                );

                // Create domain services
                _chatService = new ChatService(_eventBus, _messageIdGenerator);
                _movementService = new WorldMovementService(_playerRepository, _worldRepository);
                _playerQueryService = new PlayerQueryService();
                _worldQueryService = new WorldQueryService();

                // Register handlers in router
                var cmdRouter = new StandardCommandRouter();

                // Setup the registrars, which will register their respective commands and handlers in the router.
                var registrars = new ICommandRegistrar[]
                {
                    new ChatCommandRegistrar(_chatService),                                             // Handles chat-related commands. Uses the chat service to send messages and provide feedback on chat operations.
                    new MovementCommandRegistrar(_movementService, _worldQueryService),                 // Handles movement-related commands. Uses movement and world query services to validate moves and provide feedback.
                    new PlayerCommandRegistrar(_playerQueryService),                                    // Handles player-related commands. Uses the player query service to validate player state and attributes.
                    new SystemCommandRegistrar(_lifecycleCoordinator, _playerRepository, _eventBus)     // Handles system-related commands like server status and shutdown. Uses lifecycle coordinator to perform state changes and player repository to validate player permissions.
                };

                // Loop through registrars and register their commands in the router.
                // This is how we register commands and handlers without tightly coupling them to the router or the system initializer.
                foreach (var registrar in registrars)
                {
                    registrar.Register(cmdRouter);
                }

                // Create a standard command parser.
                ICommandParser cmdParser = new StandardCommandParser();

                // Create a standard command context builder.
                ICommandContextBuilder cmdContextBuilder = new StandardCommandContextBuilder(_playerRepository, _worldRepository);

                // Create the auth pipeline dependencies
                IAuthenticationService authService = new InMemoryAuthenticationService(_eventBus, _sessionIdGenerator);
                IAccountService accountService = new InMemoryAccountService();

                // Create the authentication pipeline
                IAuthenticationPipeline authPipeline = new StandardAuthenticationPipeline(authService, accountService, _networkSupervisor, _eventBus, _messageIdGenerator, _playerRepository, _worldRepository);

                // Create the fist pass policy list and add the authentication policy to it, so that authentication will be processed before any commands are routed.
                List<IFirstPassPolicy> firstPassPolicies = new List<IFirstPassPolicy>();
                firstPassPolicies.Add(new AuthenticationPolicy(authService));

                // Create the second pass policy list and add the muted player policy to it
                List<ISecondPassPolicy> secondPassPolicies = new List<ISecondPassPolicy>();
                secondPassPolicies.Add(new MutedPlayerPolicy());

                // Create orchestrator with fully-wired router
                _commandPipelineOrchestrator = new CommandPipelineOrchestrator(
                    _eventBus,              // Provide a reference to the event bus for publishing command processing events.
                    _networkSupervisor,     // Provide a reference to the network supervisor for sending responses back to clients.
                    cmdRouter,              // Provide the fully configured command router.
                    cmdParser,              // Provide the command parser for parsing raw input into commands.
                    _messageIdGenerator,    // Provide the message ID generator for generating unique IDs for command processing events and messages.
                    cmdContextBuilder,      // Provide the command context builder for building the context required for command handling.
                    authPipeline,           // Provide the authentication pipeline for handling authentication as part of command processing.
                    firstPassPolicies,      // Provide the list of first pass policies to be applied during command processing.
                    secondPassPolicies      // Provide the list of second pass policies to be applied during command processing.
                );

                // Register components for managed lifecycle
                RegisterLifecycleComponent(_networkSupervisor);
                RegisterLifecycleComponent(_commandPipelineOrchestrator);

                // Wire up the network supervisor to the commandPipeline
                _networkSupervisor.SetCommandPipeline(_commandPipelineOrchestrator);

                // Subscribe to server state change requests so we can trigger lifecycle coordinator state changes in response to commands.
                _eventBus.Subscribe<SystemEvents.Commands.ServerStateChangeRequest>(EventMessageType.System, OnServerStateChangeRequested);
            }
            catch (ArgumentNullException ex)
            {
                throw new InvalidOperationException($"Failed to initialize system services: missing required dependency. {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize system services. Check inner exception for details.", ex);
            }
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

        public void StopServer()
        {
            // Stop the lifecycle coordinator, which will in turn stop all registered stoppable items.
            _lifecycleCoordinator.ShutdownServer();
        }

        public IEventBus GetEventBus()
        {
            return _eventBus;
        }

        private void OnServerStateChangeRequested(SystemEvents.Commands.ServerStateChangeRequest evt)
        {
            if(_lifecycleCoordinator == null)
            {
                // This should never happen, but just in case, we check for null and log an error if it is.
                _eventBus.Publish(EventMessageType.System,
                    new SystemEvents.Errors.SystemErrorEvent("Server state change failed, Lifecycle coordinator is not initialized."));
                return;
            }

            if(evt == null)
            {
                _eventBus.Publish(EventMessageType.System,
                    new SystemEvents.Errors.SystemErrorEvent("Server state change failed, event data is null."));
                return;
            }

            if(evt.previousState != _lifecycleCoordinator.CurrentState)
            {
                _eventBus.Publish(EventMessageType.System,
                    new SystemEvents.Errors.SystemErrorEvent($"Server state change failed, current state does not match event data. Current: {_lifecycleCoordinator.CurrentState}, Event Previous: {evt.previousState}"));
                return;
            }

            bool ok = _lifecycleCoordinator.SetState(evt.newState);
            if (!ok)
            {
                // Publish an error event if the state change failed
                _eventBus.Publish(EventMessageType.System,
                    new SystemEvents.Errors.SystemErrorEvent($"Server state change failed, could not change state to {evt.newState} from current state."));
            }
        }

        public IServerLifecycle LifecycleCoordinator => _lifecycleCoordinator;
    }
}
