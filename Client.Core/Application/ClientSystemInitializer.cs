using Client.Core.MessagePipeline;
using Client.Core.Network;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Logging;
using Shared.Network.Transport;

namespace Client.Core.Application
{
    public class ClientSystemInitializer
    {
        private readonly MessagePipelineOrchestrator _messageRouter;
        private readonly LifecycleCoordinator _lifecycleCoordinator;

        private readonly IEventBus _eventBus;
        private ClientNetworkService? _networkService;
        private MessagePipelineOrchestrator? _inboundMsgRouter;

        public ClientSystemInitializer()
        {
            try
            {
                // Initialize services
                _eventBus = new BasicEventBus();
                var protocolLimits = new MuddyProtocolLimits();
                var packetSerializer = new MuddyPacketSerializer(protocolLimits);
                var packetFactory = new MuddyPacketFactory();

                //Use the LogPathHelper to safely create a location for the log file. 
                string logFilePath = LogPathHelper.CreateTimestampedLogPath("client_log");

                // Instantiate the file logger with the event bus and log file path, and the log level as a filter.
                FileLogger _fileLogger = new FileLogger(
                    _eventBus,      // Central event bus for communication between components
                    LogLevel.Debug, // Log level to filter which messages get logged
                    logFilePath     // Path to the log file where messages will be written
                );

                _networkService = new ClientNetworkService(
                    _eventBus,          // Central event bus for communication between components
                    packetSerializer,   // Responsible for converting between raw bytes and structured packets
                    packetFactory,      // Responsible for creating packet instances based on protocol definitions
                    protocolLimits      // Defines limits like max packet size, max connections, etc.
                );

                // Instantiate the message router and register handlers for different message types
                _inboundMsgRouter = new MessagePipelineOrchestrator(_eventBus);

                // Setup the registrars, which will register their respective commands and handlers in the router.
                var registrars = new MessageRouting.IMessageRegistrar[]
                {
                    new MessageRouting.Registrars.ChatCommandRegistrar(_eventBus),                                             // Handles chat-related commands. Uses the chat service to send messages and provide feedback on chat operations.
                    new MovementCommandRegistrar(_movementService, _worldQueryService),                 // Handles movement-related commands. Uses movement and world query services to validate moves and provide feedback.
                    new PlayerCommandRegistrar(_playerQueryService),                                    // Handles player-related commands. Uses the player query service to validate player state and attributes.
                    new SystemCommandRegistrar(_lifecycleCoordinator, _playerRepository, _eventBus)     // Handles system-related commands like server status and shutdown. Uses lifecycle coordinator to perform state changes and player repository to validate player permissions.
                };

                // Loop through registrars and register their commands in the router.
                // This is how we register commands and handlers without tightly coupling them to the router or the system initializer.
                foreach (var registrar in registrars)
                {
                    registrar.Register(_inboundMsgRouter);
                }

                // Register handlers for all message types
                _inboundMsgRouter.RegisterHandler("Chat", new ChatMessageHandler());
                _inboundMsgRouter.RegisterHandler("Event", new EventMessageHandler());
                _inboundMsgRouter.RegisterHandler("BinaryTransfer", new BinaryTransferHandler());
                _inboundMsgRouter.RegisterHandler("Error", new ErrorMessageHandler());
                _inboundMsgRouter.RegisterHandler("Response", new ResponseMessageHandler());
                _inboundMsgRouter.RegisterHandler("Authentication", new AuthSuccessMessageHandler());

                // Update endpoint
                string address = ServerAddressBox.Text;
                if (!int.TryParse(ServerPortBox.Text, out int port))
                {
                    AppendGameOutput("Invalid port number.", "#FFFF6B6B");
                    return;
                }

                _networkService.UpdateEndpoint(address, port);



                // Subscribe to packet log events to forward to orchestrator
                var subscriptionToken = _eventBus.Subscribe<EventEnvelope>(EventMessageType.PacketLog, OnPacketReceived);

                // Connect
                AppendGameOutput($"Connecting to {address}:{port}...", "#FFDAA520");
                await _networkService.ConnectAsync();

                _isConnected = true;
                UpdateConnectionStatus(true);
                AppendGameOutput("Connected successfully!", "#FF6BCF7F");

                // Start the orchestrator
                _inboundMsgRouter.Start();
            }
            catch (Exception ex)
            {
                AppendGameOutput($"Connection failed: {ex.Message}", "#FFFF6B6B");
                _isConnected = false;
                UpdateConnectionStatus(false);
            }


        }
    }
}
