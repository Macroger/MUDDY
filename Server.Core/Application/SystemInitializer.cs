// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Client.Core.Network;
using Server.Core.CommandPipeline;
using Server.Core.CommandPipeline.Authentication;
using Server.Core.CommandPipeline.CommandHandler;
using Server.Core.CommandPipeline.CommandRouter;
using Server.Core.CommandPipeline.ContextBuilder;
using Server.Core.CommandPipeline.Parser;
using Server.Core.CommandPipeline.Policies;
using Server.Core.Domain.Authentication;
using Server.Core.Domain.Services.ChatService;
using Server.Core.Domain.Services.PlayerQueryService;
using Server.Core.Domain.Services.WorldMovementService;
using Server.Core.Domain.Services.WorldQueryService;
using Server.Core.Infrastructure.Identity.MessageId;
using Server.Core.Infrastructure.Identity.SessionId;
using Server.Core.Infrastructure.Lifecycle;
using Server.Core.Network.Supervisor;
using Server.Core.Persistence;
using Shared.EventBus;
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

        private readonly PacketLogger _packetLogger;

        public SystemInitializer()
        {
            try
            {
                // DIAGNOSTIC: Track initialization progress
                try
                {
                    System.IO.File.AppendAllText(@"C:\temp\server_app_started.txt", $"\nSystemInitializer constructor started at {DateTime.Now}");
                }
                catch { }

                _eventBus = new BasicEventBus();

                _messageIdGenerator = new MessageIdGenerator();
                _sessionIdGenerator = new SessionIdGenerator();

                _lifecycleCoordinator = new LifecycleCoordinator(_eventBus);

                _playerRepository = new InMemoryPlayerRepository(_eventBus);
                _worldRepository = new InMemoryWorldRepository(_eventBus);

                // DIAGNOSTIC: Before logging
                try
                {
                    System.IO.File.AppendAllText(@"C:\temp\server_app_started.txt", $"\nAbout to create packet logger at {DateTime.Now}");
                }
                catch { }

                // Initialize packet logging to file with timestamp to avoid file locking issues
                // Create packet logger with file writer using safe path
                string logFilePath = Shared.Logging.LogPathHelper.CreateTimestampedLogPath("server_packets");

                // DIAGNOSTIC: Log file path determined
                try
                {
                    System.IO.File.AppendAllText(@"C:\temp\server_app_started.txt", $"\nLog file path: {logFilePath}");
                }
                catch { }

                var packetLogFileWriter = new StandardLogFileWriter(logFilePath, append: true);
                _packetLogger = new PacketLogger(_eventBus, packetLogFileWriter);

                // DIAGNOSTIC: After logging
                try
                {
                    System.IO.File.AppendAllText(@"C:\temp\server_app_started.txt", $"\nPacket logger created successfully at {DateTime.Now}");
                }
                catch { }

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

                // Create handlers and wrap services

                var chatHandler = new ChatCommandHandler(_chatService);
                var movementHandler = new MovementCommandHandler(_movementService, _worldQueryService);
                var playerHandler = new PlayerCommandHandler(_playerQueryService);
                var serverStateHandler = new ServerStateCommandHandler(_lifecycleCoordinator);
                var imageHandler = new ImageTransferCommandHandler();
                var logoutHandler = new LogoutCommandHandler(_playerRepository, _eventBus);

                // Register handlers in router
                var cmdRouter = new StandardCommandRouter();

                // Chat commands
                cmdRouter.RegisterHandler("say", chatHandler);

                // Movement commands
                cmdRouter.RegisterHandler("move", movementHandler);
                cmdRouter.RegisterHandler("go", movementHandler);
                cmdRouter.RegisterHandler("look", movementHandler);

                // Directional movement shortcuts
                cmdRouter.RegisterHandler("north", movementHandler);
                cmdRouter.RegisterHandler("south", movementHandler);
                cmdRouter.RegisterHandler("east", movementHandler);
                cmdRouter.RegisterHandler("west", movementHandler);
                cmdRouter.RegisterHandler("northeast", movementHandler);
                cmdRouter.RegisterHandler("northwest", movementHandler);
                cmdRouter.RegisterHandler("southeast", movementHandler);
                cmdRouter.RegisterHandler("southwest", movementHandler);
                cmdRouter.RegisterHandler("up", movementHandler);
                cmdRouter.RegisterHandler("down", movementHandler);

                // Player info commands
                cmdRouter.RegisterHandler("status", playerHandler);
                cmdRouter.RegisterHandler("who", playerHandler);
                cmdRouter.RegisterHandler("player", playerHandler);

                // System commands
                cmdRouter.RegisterHandler("serverstate", serverStateHandler);
                cmdRouter.RegisterHandler("sendimage", imageHandler);
                cmdRouter.RegisterHandler("logout", logoutHandler);

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

                _eventBus.Subscribe<ServerStateChangeRequestedEvent>(EventMessageType.System, OnServerStateChangeRequested);
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

        private void OnServerStateChangeRequested(ServerStateChangeRequestedEvent evt)
        {
            bool ok = _lifecycleCoordinator.SetState(evt.RequestedState);
            if (!ok)
            {
                // Publish an error event if the state change failed
                _eventBus.Publish(EventMessageType.Error,
                    new EventReason("Server state change failed", $"Could not change state to {evt.RequestedState} from current state."));
            }
        }

        public IServerLifecycle LifecycleCoordinator => _lifecycleCoordinator;
    }
}
