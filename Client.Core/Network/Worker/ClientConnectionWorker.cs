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
using System.Net.Sockets;
using System.Threading.Channels;


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
        private readonly Channel<PacketEnvelope> _sendChannel = Channel.CreateUnbounded<PacketEnvelope>();
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
            _receiveBuffer = new List<byte>();
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Starts the worker, establishes the TCP connection, and begins send/receive loops.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the connection was established and loops started;
        /// <see langword="false"/> if the worker was already running.
        /// </returns>
        /// <exception cref="SocketException">
        /// Thrown if the TCP connection cannot be established (server unreachable, connection refused, etc.).
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the server address is invalid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the network stream cannot be obtained or the connection is not properly established.
        /// </exception>
        /// <remarks>
        /// If any error occurs during connection setup, all resources are cleaned up and the exception
        /// is re-thrown to the caller. The worker state remains <see langword="false"/>.
        /// Errors that occur during send/receive loop execution are raised via <see cref="ErrorOccurred"/> event.
        /// </remarks>
        public bool Start()
        {

            if (IsRunning)
            {                
                return false;
            }

            TcpClient tcpClient = new TcpClient();
            try
            {
                // Attempt connection to server
                tcpClient.Connect(_serverAddress, _serverPort);

                NetworkStream networkStream = tcpClient.GetStream();

                // Establish connection container
                _establishedConnection = new EstablishedConnection
                {
                    TcpClient = tcpClient,
                    NetworkStream = networkStream,
                    RemoteEndPoint = (System.Net.IPEndPoint)tcpClient.Client.RemoteEndPoint!
                };

                // Start async loops — only after connection is fully established
                _sendTask = SendLoopAsync(_cancellationToken);
                _receiveTask = ReceiveLoopAsync(_cancellationToken);

                // Mark as running only after all setup succeeds
                IsRunning = true;

                return true;
            }
            catch
            {
                // Clean up resources if any step fails
                try
                {
                    tcpClient?.Close();
                    tcpClient?.Dispose();
                }
                catch
                {
                    // Suppress cleanup exceptions to preserve the original error
                }

                // Re-throw connection error to caller
                throw;
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

            // Complete the channel writer to signal no more items will be written
            _sendChannel.Writer.Complete();

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
        /// <returns>True if accepted; false if not running or queue is full.</returns>
        public bool SendEnvelope(PacketEnvelope envelope)
        {
            if (!IsRunning)
            {
                return false;
            }

            try
            {
                return _sendChannel.Writer.TryWrite(envelope);
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
                await foreach (PacketEnvelope envelope in _sendChannel.Reader.ReadAllAsync(ct))
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