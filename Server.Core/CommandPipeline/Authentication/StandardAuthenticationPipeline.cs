using Server.Core.CommandPipeline.Types;
using Server.Core.Domain.Authentication;
using Server.Core.Domain.World;
using Server.Core.Infrastructure.Identity.MessageId;
using Server.Core.Network.Supervisor;
using Server.Core.Persistence;
using Shared.Domain.Player;
using Shared.EventBus;
using Shared.Identity;
using Shared.Protocol.Transport;
using System.Text;
using System.Text.Json;
using static Shared.EventBus.DomainEvents.PlayerEvents;

namespace Server.Core.CommandPipeline.Authentication
{

    /// <summary>
    /// Handles login and register commands and creating sessions for authenticated players.
    /// </summary>
    public class StandardAuthenticationPipeline : IAuthenticationPipeline
    {
        private readonly IAuthenticationService _authService;
        private readonly IAccountService _accountService;
        private readonly INetworkSupervisor _networkSupervisor;
        private readonly IEventBus _eventBus;
        private readonly IMessageIdGenerator _messageIdGenerator;
        private readonly IPlayerRepository _playerRepository;
        private readonly IWorldRepository _worldRepository;

        public StandardAuthenticationPipeline(
            IAuthenticationService authService,
            IAccountService accountService,
            INetworkSupervisor networkSupervisor,
            IEventBus eventBus,
            IMessageIdGenerator messageIdGenerator,
            IPlayerRepository playerRepository,
            IWorldRepository worldRepository)
        {
            _authService = authService;
            _accountService = accountService;
            _networkSupervisor = networkSupervisor;
            _eventBus = eventBus;
            _messageIdGenerator = messageIdGenerator;
            _playerRepository = playerRepository;
            _worldRepository = worldRepository;
        }

        /// <summary>
        /// Processes an authentication command.
        /// Parses the command, validates it, and creates a session if successful.
        /// </summary>
        public async Task ProcessAuthCommandAsync(TransportEnvelope envelope)
        {
            try
            {


                // Parse the JSON payload to extract command and arguments
                string json = Encoding.UTF8.GetString(envelope.Payload);
                var cmdJson = JsonSerializer.Deserialize<JsonCommand>(json);

                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Authentication,
                    new EventReason($"Received authentication command: {cmdJson?.Verb}")
                );

                if (cmdJson == null)
                {
                    SendAuthError(envelope.ConnId, "Invalid command format");
                    return;
                }

                // Route to appropriate handler based on command verb
                switch (cmdJson.Verb?.ToLowerInvariant())
                {
                    case "login":
                        await HandleLoginAsync(envelope.ConnId, cmdJson.Args);
                        break;

                    case "register":
                        await HandleRegisterAsync(envelope.ConnId, cmdJson.Args);
                        break;

                    default:
                        SendAuthError(envelope.ConnId, $"Unknown authentication command: {cmdJson.Verb}");
                        break;
                }
            }
            catch (Exception ex)
            {
                SendAuthError(envelope.ConnId, $"Authentication error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles login: validates credentials and creates a session for the player.
        /// </summary>
        private async Task HandleLoginAsync(ConnectionId connId, string[]? args)
        {
            // Validate we have username and password arguments
            if (args == null || args.Length < 2)
            {
                SendAuthError(connId, "Login requires username and password. Usage: login <username> <password>");
                return;
            }

            string username = args[0];
            string password = args[1];

            // Validate the credentials against the account service
            bool credentialsValid = await _accountService.ValidateCredentialsAsync(username, password);

            if (!credentialsValid)
            {
                SendAuthError(connId, "Invalid username or password");
                return;
            }

            // Credentials are valid - create a session for this player
            SessionId sessionId = await _authService.CreateSessionAsync(connId, username);

            // Spawn the player into the world so the command pipeline can find them
            await SpawnPlayerAsync(connId, username);

            // Send success response with the new SessionToken
            SendAuthSuccess(connId, sessionId, $"Welcome back, {username}!");
        }

        /// <summary>
        /// Handles register: creates a new account and starts a session for the player.
        /// </summary>
        private async Task HandleRegisterAsync(ConnectionId connId, string[]? args)
        {
            // Validate we have username and password arguments
            if (args == null || args.Length < 2)
            {
                SendAuthError(connId, "Register requires username and password. Usage: register <username> <password>");
                return;
            }

            string username = args[0];
            string password = args[1];

            // Try to register the new account
            bool accountCreated = await _accountService.RegisterAccountAsync(username, password);

            if (!accountCreated)
            {
                SendAuthError(connId, "Username already exists. Please choose a different name.");
                return;
            }

            // Account created successfully - create a session for the new player
            SessionId sessionId = await _authService.CreateSessionAsync(connId, username);

            // Spawn the player into the world so the command pipeline can find them
            await SpawnPlayerAsync(connId, username);

            // Send success response with the new SessionToken
            SendAuthSuccess(connId, sessionId, $"Account created! Welcome, {username}!");
        }

        /// <summary>
        /// Creates a <see cref="PlayerState"/> for the authenticated player and places them
        /// in the starting room so the command pipeline can find them immediately after login.
        /// </summary>
        private async Task SpawnPlayerAsync(ConnectionId connId, string username)
        {
            RoomId startingRoom = GameWorldFactory.StartingRoomId;

            PlayerState newPlayer = new PlayerState
            {
                ConnId = connId,
                PlayerName = username,
                CurrentLocation = startingRoom,
                ActiveConditions = new HashSet<PlayerCondition>()
            };

            await _playerRepository.UpsertPlayerAsync(newPlayer);

            // Add the player to the starting room's presence list
            RoomState? room = await _worldRepository.GetRoomAsync(startingRoom);
            if (room != null)
            {
                HashSet<ConnectionId> updatedPlayers = new HashSet<ConnectionId>(room.PlayersPresent) { connId };
                RoomState updatedRoom = new RoomState(
                    room.Id,
                    room.Description,
                    room.Conditions,
                    updatedPlayers,
                    room.Exits);
                await _worldRepository.UpdateRoomAsync(updatedRoom);

                EventBusHelper.PublishEvent<PlayerEnteredWorldEvent>(
                _eventBus,
                EventMessageType.World,
                new PlayerEnteredWorldEvent(connId, username, updatedRoom.Id)
                );
            }
        }

        /// <summary>
        /// Sends an authentication error response.
        /// </summary>
        private void SendAuthError(ConnectionId connId, string message)
        {
            string payload = $"ERROR: {message}";

            EventReason eventReason = new(payload);

            EventBusHelper.PublishEvent(
                _eventBus,
                EventMessageType.Error,
                eventReason
            );

            var response = new TransportEnvelope(
                messageId: _messageIdGenerator.New(),
                messageType: TransportMessageType.Error,
                flags: Shared.Protocol.Types.MessageFlags.None,
                payload: Encoding.UTF8.GetBytes(payload),
                connectionId: connId,
                sessionId: SessionId.Unauthenticated  // No session for error
            );

            _networkSupervisor.SendToClient(connId, response);
        }

        /// <summary>
        /// Sends an authentication success response to the specified client connection.
        /// </summary>
        /// <param name="connId">The identifier of the client connection to which the authentication success message is sent.</param>
        /// <param name="sessionId">The session identifier associated with the authenticated client.</param>
        /// <param name="message">A message describing the authentication success.</param>
        private void SendAuthSuccess(ConnectionId connId, SessionId sessionId, string message)
        {
            string payload = $"SUCCESS: {message}";

            EventReason eventReason = new(payload);

            EventBusHelper.PublishEvent(
                _eventBus,
                EventMessageType.Authentication,
                eventReason
            );

            var envelope = new TransportEnvelope(
                messageId: _messageIdGenerator.New(),
                messageType: TransportMessageType.AuthSuccess,
                flags: Shared.Protocol.Types.MessageFlags.None,
                payload: Encoding.UTF8.GetBytes(payload),
                connectionId: connId,
                sessionId: sessionId
            );

            _networkSupervisor.SendToClient(connId, envelope);
        }
    }
}
