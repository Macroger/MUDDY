// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Identity;
using Shared.Network.Transport;
using Shared.Network.Types;
using Client.Core.Events;
using System.Net.Sockets;

namespace Client.Core.Network
{

    /// <summary>
    /// Handles all client-side network communication with the server.
    /// Responsible for connecting, disconnecting, sending, and receiving packets.
    /// Publishes network events to the event bus and manages the session lifecycle.
    /// </summary>
    public class ClientNetworkService
    {

        /// <summary>
        /// The server address to connect to. Can be set by the GUI.
        /// </summary>
        public string ServerAddress { get; private set; } = "127.0.0.1";

        /// <summary>
        /// The server port to connect to. Can be set by the GUI.
        /// </summary>
        public int ServerPort { get; private set; } = 30333;

        // Used to publish network events (e.g., connection, errors, packets)
        private readonly IEventBus _eventBus;

        // Serializes/deserializes packets for network transmission
        private readonly IPacketSerializer _packetSerializer;

        // Creates packets from envelopes
        private readonly IPacketFactory _packetFactory;

        // Protocol limits (e.g., max packet size)
        private readonly MuddyProtocolLimits _protocolLimits;

        // TCP client for the network connection
        private TcpClient? _tcpClient;

        // Stream for reading/writing network data
        private NetworkStream? _networkStream;

        // Used to cancel network operations (e.g., on disconnect)
        private CancellationTokenSource? _cts;

        // Tracks the current session ID (set after login/handshake)
        private SessionId _sessionId = SessionId.Unauthenticated;

        /// <summary>
        /// Gets or sets the current session ID. Fires OnSessionEstablished when session is set for the first time.
        /// </summary>
        public SessionId SessionId
        {
            get => _sessionId;
            set
            {
                // Only fire event the first time a valid session is set
                if (_sessionId == SessionId.Unauthenticated && value != SessionId.Unauthenticated)
                {
                    _sessionId = value;
                    OnSessionEstablished?.Invoke(value);
                }
                else
                {
                    _sessionId = value;
                }
            }
        }

        /// <summary>
        /// Event fired when the session is established (first time SessionId is set to a valid value).
        /// Subscribe to this to react to successful login/handshake.
        /// </summary>
        public event Action<SessionId>? OnSessionEstablished;

        /// <summary>
        /// Constructor. Injects dependencies for event bus, packet serialization, and protocol limits.
        /// </summary>
        public ClientNetworkService(IEventBus eventBus,
            IPacketSerializer packetSerializer,
            IPacketFactory packetFactory,
            MuddyProtocolLimits protocolLimits)
        {
            _eventBus = eventBus;
            _packetSerializer = packetSerializer;
            _packetFactory = packetFactory;
            _protocolLimits = protocolLimits;

            // Subscribe to connect/disconnect requests from the event bus
            _eventBus.Subscribe<ClientNetworkEvents.Commands.ConnectToServer>(EventMessageType.Gui, OnConnectRequested);
            _eventBus.Subscribe<ClientNetworkEvents.Commands.DisconnectFromServer>(EventMessageType.Gui, OnDisconnectRequested);
            _eventBus.Subscribe<ClientNetworkEvents.Commands.UpdateConnectionEndpoint>(EventMessageType.Gui, OnUpdateEndpointRequested);
        }

        /// <summary>
        /// Updates the server address and port. Throws if called while connected.
        /// </summary>
        public void UpdateEndpoint(string serverAddress, int serverPort)
        {
            if (IsConnected)
                throw new InvalidOperationException("Cannot update endpoint while connected. Please disconnect first.");

            // Validate address and port
            if (!ValidateConnectionParameters(serverAddress, serverPort, out string errorMessage))
            {
                _eventBus.Publish(EventMessageType.Network,
                    new NetworkEvents.Errors.NetworkError(errorMessage));
                return;
            }

            ServerAddress = serverAddress;
            ServerPort = serverPort;
        }

        private void OnUpdateEndpointRequested(ClientNetworkEvents.Commands.UpdateConnectionEndpoint endpoint)
        {
            if (endpoint == null)
            {
                _eventBus.Publish(EventMessageType.Network,
                    new NetworkEvents.Errors.NetworkError("Invalid endpoint update request: data is null"));

                return;
            }

            UpdateEndpoint(endpoint.address, endpoint.port);
        }

        /// <summary>
        /// Handles connect requests from the event bus.
        /// </summary>
        private void OnConnectRequested(BusEvent evt)
        {
            _ = ConnectAsync();
        }

        /// <summary>
        /// Handles disconnect requests from the event bus.
        /// </summary>
        private void OnDisconnectRequested(BusEvent evt)
        {
            _ = DisconnectAsync();
        }

        /// <summary>
        /// Indicates whether the client is currently connected to the server.
        /// </summary>
        public bool IsConnected { get; private set; } = false;
   
        /// <summary>
        /// Connects to the server using the current ServerAddress and ServerPort.
        /// Starts the receive loop and publishes a connected event.
        /// </summary>
        public async Task ConnectAsync()
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ServerAddress, ServerPort);
                _networkStream = _tcpClient.GetStream();
                _cts = new CancellationTokenSource();

                IsConnected = true;

                // Emit an event to notify listeners that we are connected
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.ConnectionStatusChangedEvent
                    (
                        true,
                        $"Connected to server {ServerAddress}:{ServerPort}"
                    ));

                // Start listening for incoming packets
                _ = ReceiveLoopAsync();
            }
            catch (Exception ex)
            {               
                IsConnected = false;
                var connectionStatusEvent = new ClientNetworkEvents.ConnectionStatusChangedEvent
               (
                   false,
                   $"Failed to connect to server {ServerAddress}:{ServerPort}: {ex.Message}"
               );
                _eventBus.Publish(
                    EventMessageType.Network,
                    connectionStatusEvent);

                throw;
            }
        }

        /// <summary>
        /// Disconnects from the server and cleans up resources.
        /// Publishes a disconnected event.
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                _cts?.Cancel();
                _networkStream?.Close();
                _tcpClient?.Close();

                IsConnected = false;

                // Notify listeners that we are disconnected
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.ConnectionStatusChangedEvent(
                        false,
                        "Disconnected from server")
                );
            }
            catch (Exception ex)
            {
                IsConnected = false;

                // Notify listeners of disconnect error
                _eventBus.Publish(
                    EventMessageType.Network,
                    new ClientNetworkEvents.ConnectionStatusChangedEvent(
                        false,
                        $"Disconnected from server with errors: {ex.Message}")
                );
            }
        }

        /// <summary>
        /// Sends a message (TransportEnvelope) to the server.
        /// Serializes the envelope, writes it to the network, and publishes a sent event.
        /// </summary>
        public async Task SendMessageAsync(TransportEnvelope envelope)
        {
            if (_networkStream == null)
                throw new InvalidOperationException("Not connected to server.");

            try
            {
                // Convert envelope to packet and serialize
                var packet = _packetFactory.CreateMuddyPacket(envelope);
                var bytes = _packetSerializer.Serialize(packet);
                await _networkStream.WriteAsync(bytes, 0, bytes.Length);

                // Only publish to PacketLog channel for specialized logging
                _eventBus.Publish(
                    EventMessageType.Network,
                    new NetworkEvents.Packets.PacketSent(envelope)
                    );
            }
            catch (Exception ex)
            {
                // Notify listeners of send error
                _eventBus.Publish(
                    EventMessageType.Network,
                    new NetworkEvents.Errors.NetworkError
                    (
                       ErrorMessage: "Failed to send packet",
                       Exception: ex
                    )
                );

                throw;
            }
        }

        /// <summary>
        /// Continuously reads packets from the server in a background loop.
        /// Deserializes each packet, converts it to an envelope, and publishes a received event.
        /// Handles disconnects and errors.
        /// </summary>
        private async Task ReceiveLoopAsync()
        {
            if (_networkStream == null || _cts == null)
                return;

            // Buffer for incoming data (max packet size)
            var buffer = new byte[_protocolLimits.MaxBinaryPacketBytes];
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    // --- Read the packet header first ---
                    int headerSize = _protocolLimits.headerSize;
                    int bytesRead = 0;

                    // Read until we have the full header
                    while (bytesRead < headerSize)
                    {
                        int read = await _networkStream.ReadAsync(buffer, bytesRead, headerSize - bytesRead, _cts.Token);
                        if (read == 0) throw new Exception("Connection closed by remote host.");
                        bytesRead += read;
                    }

                    // Parse header to get body length
                    var header = _packetSerializer.DeserializeHeader(new ReadOnlySpan<byte>(buffer, 0, headerSize));
                    int packetSize = headerSize + (int)header.BodyLength + 4; // 4 bytes CRC

                    // --- Read the rest of the packet ---
                    while (bytesRead < packetSize)
                    {
                        int read = await _networkStream.ReadAsync(buffer, bytesRead, packetSize - bytesRead, _cts.Token);
                        if (read == 0) throw new Exception("Connection closed by remote host.");
                        bytesRead += read;
                    }

                    // Deserialize the full packet
                    var packet = _packetSerializer.Deserialize(new ReadOnlySpan<byte>(buffer, 0, packetSize));

                    // Convert to higher-level envelope
                    var envelope = ConvertPacketToEnvelope(packet);

                    // Publish the received packet event
                    _eventBus.Publish(
                        EventMessageType.Network,
                        new NetworkEvents.Packets.PacketReceived(envelope)
                    );
                }
            }
            catch (OperationCanceledException)
            {
                // Normal on disconnect
            }
            catch (Exception ex)
            {
                // Notify listeners of receive loop error
                _eventBus.Publish(
                    EventMessageType.Network,
                    new NetworkEvents.Errors.NetworkError
                    (
                       ErrorMessage: "Error in receive loop",
                       Exception: ex
                    )
                );

                await DisconnectAsync();
            }
        }

        /// <summary>
        /// Converts a MuddyPacket (raw network packet) into a TransportEnvelope (higher-level message).
        /// 1. Extracts the session ID from the packet header.
        /// 2. If this is the first valid session ID, stores it and fires the session event.
        /// 3. Returns a new TransportEnvelope with all relevant fields.
        /// </summary>
        /// <param name="packet">The raw MuddyPacket received from the network.</param>
        /// <returns>A TransportEnvelope representing the message contents.</returns>
        private TransportEnvelope ConvertPacketToEnvelope(MuddyPacket packet)
        {
            // Extract session ID from the packet header
            var sessionId = new SessionId(packet.Header.SessionId);

            // If this is the first valid session ID, store it and fire event
            if (SessionId == SessionId.Unauthenticated && sessionId != SessionId.Unauthenticated)
            {
                SessionId = sessionId;
            }

            // Build and return the envelope (higher-level message)
            return new TransportEnvelope(
                connectionId: new ConnectionId("1"),
                messageId: new MessageId(packet.Header.MsgId),
                messageType: (TransportMessageType)packet.Header.MsgType,
                flags: (MessageFlags)packet.Header.BitFlags,
                payload: packet.Body,
                sessionId: sessionId
            );
        }

        /// <summary>
        /// Validates a server address (IP or hostname) and port.
        /// </summary>
        /// <param name="address">The server address to validate.</param>
        /// <param name="port">The server port to validate.</param>
        /// <param name="errorMessage">Output parameter containing the validation error message if validation fails.</param>
        /// <returns>True if both address and port are valid; otherwise false.</returns>
        private static bool ValidateConnectionParameters(string address, int port, out string errorMessage)
        {
            errorMessage = string.Empty;

            // Validate address is not null or empty
            if (string.IsNullOrWhiteSpace(address))
            {
                errorMessage = "Server address cannot be empty";
                return false;
            }

            // Validate hostname or IP address format
            UriHostNameType hostType = Uri.CheckHostName(address);
            if (hostType == UriHostNameType.Unknown)
            {
                errorMessage = $"Invalid server address format: '{address}'";
                return false;
            }

            // Validate port range
            if (port is < 1 or > 65535)
            {
                errorMessage = $"Port must be between 1 and 65535 (got {port})";
                return false;
            }

            return true;
        }


    }
}
