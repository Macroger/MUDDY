using Client.Core.CommandPipeline;
using Shared.EventBus;
using Shared.Identity;
using Shared.Protocol.Transport;
using Shared.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

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
            _eventBus.Subscribe<ConnectRequestedEvent>(EventMessageType.Gui, OnConnectRequested);
            _eventBus.Subscribe<DisconnectRequestedEvent>(EventMessageType.Gui, OnDisconnectRequested);
        }

        /// <summary>
        /// Handles connect requests from the event bus.
        /// </summary>
        private async Task OnConnectRequestedAsync(ConnectRequestedEvent evt)
        {
            await ConnectAsync();
        }

        /// <summary>
        /// Handles disconnect requests from the event bus.
        /// </summary>
        private async Task OnDisconnectRequestedAsync(DisconnectRequestedEvent evt)
        {
            await DisconnectAsync();
        }

        /// <summary>
        /// Handles connect requests from the event bus.
        /// </summary>
        private void OnConnectRequested(ConnectRequestedEvent evt)
        {
            _ = ConnectAsync();
        }

        /// <summary>
        /// Handles disconnect requests from the event bus.
        /// </summary>
        private void OnDisconnectRequested(DisconnectRequestedEvent evt)
        {
            _ = DisconnectAsync();
        }


        /// <summary>
        /// Indicates whether the client is currently connected to the server.
        /// </summary>
        public bool IsConnected { get; private set; } = false;

        /// <summary>
        /// Connects to the server at the given host and port.
        /// Starts the receive loop and publishes a connected event.
        /// </summary>
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

                // Notify listeners that we are connected
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.ClientNetwork,
                    new EventReason($"Connected to server {ServerAddress}:{ServerPort}")
                );

                // Start listening for incoming packets
                _ = ReceiveLoopAsync();
            }
            catch (Exception ex)
            {
                IsConnected = false;
                // Notify listeners of connection failure
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason($"Failed to connect to server {ServerAddress}:{ServerPort}", ex.Message)
                );
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
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.ClientNetwork,
                    new EventReason("Disconnected from server")
                );
            }
            catch (Exception ex)
            {
                IsConnected = false;
                // Notify listeners of disconnect error
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason("Error during disconnect", ex.Message)
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

                // Notify listeners that a packet was sent
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.ClientNetwork,
                    new EventReason("Packet sent", new { envelope.MessageId, envelope.MessageType })
                );
            }
            catch (Exception ex)
            {
                // Notify listeners of send error
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason("Failed to send packet", ex.Message)
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

                    // Notify listeners that a packet was received
                    EventBusHelper.PublishEvent(
                        _eventBus,
                        EventMessageType.ClientNetwork,
                        new EventReason("Packet received", envelope)
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
                EventBusHelper.PublishEvent(
                    _eventBus,
                    EventMessageType.Error,
                    new EventReason("Error in receive loop", ex.Message)
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
    }
}
