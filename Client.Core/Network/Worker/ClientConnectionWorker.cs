// =============================================================================
/// @file       ClientConnectionWorker.cs
/// @namespace  Client.Core.Network.Worker
/// @brief      Manages single client-to-server TCP connection with send/receive loops.
/// @details    Maintains packet send queue and receive buffer. Handles packet
///             serialization/deserialization.
// =============================================================================

using Client.Core.Network.Types;
using Shared.Identity;
using Shared.Network.Transport;
using Shared.Network.Types;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Client.Core.Network.Worker
{
    /// <summary>
    /// Manages a single client-to-server TCP connection.
    /// Implements send and receive loops with packet buffering and parsing.
    /// </summary>
    public sealed class ClientConnectionWorker : IClientConnectionWorker
    {
        #region Private Fields        

        private readonly string _serverAddress = string.Empty;
        private readonly int _serverPort = 0;
        private readonly IPacketFactory _packetFactory = null!;
        private readonly IPacketEnvelopeFactory _envelopeFactory = null!;
        private readonly IPacketSerializer _packetSerializer = null!;
        private readonly MuddyProtocolLimits _protocolLimits = null!;
        private readonly CancellationToken _cancellationToken = CancellationToken.None;

        private EstablishedConnection? _establishedConnection = null;
        private BlockingCollection<PacketEnvelope> _sendQueue = new();
        private List<byte> _receiveBuffer = new();

        private volatile bool _isRunning = false;
        private int _shutdownInitiated = 0;

        private readonly ConnectionId _serverConnectionId = new ConnectionId("1");

        private Task? _sendTask = null;
        private Task? _receiveTask = null;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets a value indicating whether the worker is currently running.
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            private set => _isRunning = value;
        }

        #endregion

        #region Infrastructure Events (C# Events)

        /// <summary>
        /// Raised when a complete packet is received.
        /// </summary>
        public event EventHandler<PacketEnvelope>? PacketReceived = null;

        /// <summary>
        /// Raised when the connection closes.
        /// </summary>
        public event EventHandler? ConnectionClosed = null;

        /// <summary>
        /// Raised when an error occurs.
        /// </summary>
        public event EventHandler<Exception>? ErrorOccurred = null;

        /// <summary>
        /// Raised after a packet is successfully transmitted.
        /// </summary>
        public event EventHandler<PacketEnvelope>? PacketSent = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new client connection worker.
        /// </summary>
        /// <param name="serverAddress">The server address to connect to.</param>
        /// <param name="serverPort">The server port to connect to.</param>
        /// <param name="packetFactory">Factory for creating packets from envelopes.</param>
        /// <param name="envelopeFactory"> Factory for creating PacketEnvelopes from packets.</param>"
        /// <param name="packetSerializer">Serializer for packet serialization/deserialization.</param>
        /// <param name="protocolLimits">Protocol limits for packet validation.</param>
        /// <param name="cancellationToken">Cancellation token for shutdown.</param>
        public ClientConnectionWorker(
            string serverAddress,
            int serverPort,
            IPacketFactory packetFactory,
            IPacketEnvelopeFactory envelopeFactory,
            IPacketSerializer packetSerializer,
            MuddyProtocolLimits protocolLimits,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                throw new ArgumentException("Server address cannot be null or empty.", nameof(serverAddress));
            }

            if (serverPort <= 0 || serverPort > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(serverPort), "Port must be between 1 and 65535.");
            }

            _serverAddress = serverAddress;
            _serverPort = serverPort;
            _packetFactory = packetFactory ?? throw new ArgumentNullException(nameof(packetFactory));
            _envelopeFactory = envelopeFactory ?? throw new ArgumentNullException(nameof(envelopeFactory));
            _packetSerializer = packetSerializer ?? throw new ArgumentNullException(nameof(packetSerializer));
            _protocolLimits = protocolLimits ?? throw new ArgumentNullException(nameof(protocolLimits));
            _cancellationToken = cancellationToken;
            _sendQueue = new BlockingCollection<PacketEnvelope>(new ConcurrentQueue<PacketEnvelope>());
            _receiveBuffer = new List<byte>();
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Starts the worker, establishes connection, and begins send/receive loops.
        /// </summary>
        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            try
            {
                TcpClient tcpClient = new TcpClient();
                tcpClient.Connect(_serverAddress, _serverPort);
                NetworkStream networkStream = tcpClient.GetStream();

                _establishedConnection = new EstablishedConnection
                {
                    TcpClient = tcpClient,
                    NetworkStream = networkStream,
                    RemoteEndPoint = (System.Net.IPEndPoint)tcpClient.Client.RemoteEndPoint!
                };

                IsRunning = true;

                _sendTask = SendLoopAsync(_cancellationToken);
                _receiveTask = ReceiveLoopAsync(_cancellationToken);
            }
            catch (Exception ex)
            {
                IsRunning = false;
                ErrorOccurred?.Invoke(this, ex);
            }
        }

        /// <summary>
        /// Stops the worker and closes the connection.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            InitiateShutdown();
        }

        /// <summary>
        /// Initiates graceful shutdown of the worker (runs exactly once).
        /// </summary>
        private void InitiateShutdown()
        {
            if (Interlocked.Exchange(ref _shutdownInitiated, 1) != 0)
            {
                return;
            }

            _sendQueue.CompleteAdding();

            try
            {
                _establishedConnection?.NetworkStream?.Close();
                _establishedConnection?.TcpClient?.Close();
            }
            catch { }

            IsRunning = false;
            OnConnectionClosed();
        }

        #endregion

        #region Send and Receive

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="envelope">The envelope to send.</param>
        /// <returns>True if accepted; false if not running.</returns>
        public bool SendEnvelope(PacketEnvelope envelope)
        {
            if (!IsRunning)
            {
                return false;
            }

            try
            {
                _sendQueue.Add(envelope);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Background loop for sending enqueued messages to the server.
        /// </summary>
        private async Task SendLoopAsync(CancellationToken ct)
        {
            try
            {
                foreach (PacketEnvelope envelope in _sendQueue.GetConsumingEnumerable(ct))
                {
                    if (_establishedConnection?.NetworkStream == null)
                    {
                        break;
                    }

                    try
                    {
                        MuddyPacket packet = _packetFactory.CreateMuddyPacket(envelope);
                        byte[] serializedPacket = _packetSerializer.Serialize(packet);
                        await _establishedConnection.NetworkStream.WriteAsync(serializedPacket, ct);
                        await _establishedConnection.NetworkStream.FlushAsync(ct);

                        // Signal successful transmission to supervisor
                        PacketSent?.Invoke(this, envelope);
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.Invoke(this, ex);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
            finally
            {
                InitiateShutdown();
            }
        }

        /// <summary>
        /// Background loop for receiving bytes from server, accumulating, and parsing packets.
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            try
            {
                byte[] tempBuffer = new byte[4096];
                NetworkStream? networkStream = _establishedConnection?.NetworkStream;

                while (!ct.IsCancellationRequested && networkStream != null)
                {
                    int bytesReceived = await networkStream.ReadAsync(tempBuffer, ct);

                    if (bytesReceived == 0)
                    {
                        // Connection closed by server
                        break;
                    }

                    _receiveBuffer.AddRange(tempBuffer.AsSpan(0, bytesReceived));

                    while (_receiveBuffer.Count >= _protocolLimits.headerSize)
                    {
                        byte[] headerBytes = _receiveBuffer.GetRange(0, _protocolLimits.headerSize).ToArray();
                        MuddyPacketHeader header = _packetSerializer.DeserializeHeader(headerBytes);

                        int totalPacketSize = _protocolLimits.headerSize + (int)header.BodyLength + _protocolLimits.tailSize;

                        bool isBinary = (header.BitFlags & (ushort)MessageFlags.BinaryPayload) != 0;
                        int sizeLimit = isBinary ? _protocolLimits.MaxBinaryPacketBytes : _protocolLimits.MaxJsonPacketBytes;

                        if (totalPacketSize > sizeLimit)
                        {
                            throw new InvalidDataException(
                                $"Packet too large. Size: {totalPacketSize}, limit: {sizeLimit}");
                        }

                        if (_receiveBuffer.Count < totalPacketSize)
                        {
                            // Insufficient bytes for full packet, wait for more
                            break;
                        }

                        byte[] fullPacketBytes = _receiveBuffer.GetRange(0, totalPacketSize).ToArray();
                        _receiveBuffer.RemoveRange(0, totalPacketSize);

                        MuddyPacket packet = _packetSerializer.Deserialize(fullPacketBytes);
                        PacketEnvelope envelope = _envelopeFactory.CreateFromPacket(packet, _serverConnectionId);

                        PacketReceived?.Invoke(this, envelope);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
            finally
            {
                InitiateShutdown();
            }
        }

        #endregion

        #region Event Raising

        private void OnConnectionClosed()
        {
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}