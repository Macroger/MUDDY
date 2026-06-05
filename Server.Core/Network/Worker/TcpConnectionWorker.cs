// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.Network.Model;
using Shared.Identity;
using Shared.Network.Transport;
using Shared.Network.Types;
using System.Collections.Concurrent;

namespace Server.Core.Network.Worker
{
    /// <summary>
    /// Manages a single TCP connection: send and receive loops, packet parsing and event dispatch.
    ///
    /// The worker maintains a send queue and a byte accumulator for incoming data. It runs background
    /// tasks to receive bytes from the socket, parse complete packets using the provided packet serializer/factory
    /// and to send outgoing messages. Events are raised for received messages, connection closure and errors.
    /// </summary>
    public class TcpConnectionWorker : IConnectionWorker
    {
        #region Properties and fields

        #region Collection fields for managing send queue and receive buffer

        /// <summary>
        /// Queue of outbound protocol envelopes to be sent to the remote endpoint.
        /// </summary>
        private BlockingCollection<TransportEnvelope> sendQue = new BlockingCollection<TransportEnvelope>(new ConcurrentQueue<TransportEnvelope>());

        /// <summary>
        /// Accumulator buffer used for assembling bytes read from the socket until complete packets are available.
        /// </summary>
        private List<byte> _byteAccumulatorBuffer = new List<byte>();

        #endregion

        #region Core dependencies for managing the connection, packet processing, and serialization

        private AcceptedConnection _acceptedConnection;
        private MuddyProtocolLimits _packetLimits;

        private IPacketFactory _packetFactory;
        private IPacketSerializer _packetSerializer;

        #endregion

        #region Lifecycle management fields

        private volatile bool _isRunning;
        private int _shutdownInitiated = 0;
        private CancellationToken _ct;

        #endregion

        #region Tasks for send and receive loops

        private Task? _sendTask;
        private Task? _receiveTask;

        #endregion

        #region Public properties

        /// <summary>The connection identifier associated with this worker.</summary>
        public ConnectionId ConnId { get; init; }

        /// <summary>Indicates whether the worker is currently running.</summary>
        public bool IsRunning
        {
            get => _isRunning;
            private set => _isRunning = value;
        }

        #endregion        

        #region Events for message reception, connection closure, and error reporting

        /// <summary>Raised when a complete protocol message has been received and parsed.</summary>
        public event EventHandler<TransportEnvelope>? MessageReceived;

        /// <summary>Raised when the connection has been closed or the worker has shut down.</summary>
        public event EventHandler? ConnectionClosed;

        /// <summary>Raised when an error occurs within the worker.</summary>
        public event EventHandler<Exception>? ErrorOccurred;

        #endregion

        #endregion

        #region Methods

        #region Constructor to initialize the connection worker with necessary dependencies and configuration
        /// <summary>
        /// Constructs a new <see cref="TcpConnectionWorker"/>.
        /// </summary>
        /// <param name="acceptedConnection">Information about the accepted connection including the socket and connection id.</param>
        /// <param name="cts">Cancellation token used to signal shutdown for this worker.</param>
        /// <param name="packetFactory">Factory used to create wire packets from protocol envelopes for sending.</param>
        /// <param name="packetSerializer">Serializer/deserializer for packet headers and bodies.</param>
        /// <param name="limits">Protocol limits (header size, tail size, max packet bytes) used for validation.</param>
        public TcpConnectionWorker(
            AcceptedConnection acceptedConnection,
            CancellationToken cts,
            IPacketFactory packetFactory,
            IPacketSerializer packetSerializer,
            MuddyProtocolLimits limits)
        {
            _acceptedConnection = acceptedConnection;
            ConnId = acceptedConnection.connId;
            _ct = cts;
            _packetFactory = packetFactory;
            _packetSerializer = packetSerializer;
            _packetLimits = limits;
        }
        #endregion

        #region Lifecycle management methods for starting and stopping the connection worker

        /// <summary>
        /// Starts the worker's send and receive background loops.
        /// </summary>
        /// <remarks>If the worker is already running this method is a no-op. Background tasks for sending and receiving are started using the provided cancellation token.</remarks>
        public void Start()
        {
            // If the connection is already running, there's no need to start it again
            if (IsRunning) return;

            IsRunning = true;

            _sendTask = SendLoopAsync(_ct);
            _receiveTask = ReceiveLoopAsync(_ct);
        }

        /// <summary>
        /// Requests the worker to stop and initiates shutdown.
        /// </summary>
        /// <remarks>If the worker is not running this method is a no-op. Otherwise it triggers the shutdown sequence which will stop sending and close the socket.</remarks>
        public void Stop()
        {
            // If the connection is not running, there's nothing to stop
            if (!IsRunning) return;

            InitiateShutdown();
        }

        /// <summary>
        /// Performs the one-time shutdown sequence for the worker.
        /// </summary>
        /// <param name="cause">Optional exception that triggered the shutdown.</param>
        /// <remarks>Ensures shutdown runs exactly once, completes the send queue, closes the socket and raises the connection closed event.</remarks>
        private void InitiateShutdown(Exception? cause = null)
        {
            // Ensure shutdown logic runs exactly once
            if (Interlocked.Exchange(ref _shutdownInitiated, 1) != 0)
                return;

            // Stop new outbound messages
            sendQue.CompleteAdding();

            try
            {
                _acceptedConnection.clientSocket.Close();
            }
            catch { }

            IsRunning = false;

            OnConnectionClosed();
        }

        #endregion

        #region Send and Receive messages

        /// <summary>
        /// Enqueues a protocol envelope for sending to the remote endpoint.
        /// </summary>
        /// <param name="msg">The protocol envelope to send.</param>
        /// <returns>true if the message was accepted for sending; false if the worker is not running or the queue cannot accept the message.</returns>
        public bool SendMessage(TransportEnvelope msg)
        {
            if (!IsRunning)
                return false;

            try
            {
                sendQue.Add(msg);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Background loop that receives bytes from the socket, accumulates them and parses complete packets.
        /// </summary>
        /// <param name="ct">Cancellation token that signals the loop to stop.</param>
        /// <returns>A task representing the lifetime of the receive loop.</returns>
        /// <remarks>Reads from the socket into a temporary buffer, appends bytes to the accumulator and attempts to parse complete packets. Partial packets are left in the accumulator until more bytes arrive. Oversized packets are rejected by throwing an InvalidDataException.</remarks>
        private Task ReceiveLoopAsync(CancellationToken ct)
        {
            return Task.Run(() =>
            {
                try
                {
                    // Small buffer — appropriate for JSON traffic.
                    byte[] tempBuffer = new byte[4096];

                    while (!ct.IsCancellationRequested)
                    {
                        // Read bytes from socket - BLOCKING CALL - will wait until data is available or connection is closed
                        int bytesReceived = _acceptedConnection.clientSocket.Receive(tempBuffer);

                        // If bytesReceived is 0, it means the connection has been gracefully closed by the client
                        if (bytesReceived == 0) break;

                        // Append received bytes to the accumulator buffer
                        _byteAccumulatorBuffer.AddRange(tempBuffer.AsSpan(0, bytesReceived));

                        // Once enough bytes have accumulated to contain at least a full header, we can attempt to parse packets.
                        // Keep looping until we consume all complete packets in the buffer.
                        while (_byteAccumulatorBuffer.Count >= _packetLimits.headerSize)
                        {
                            // Copy the header bytes to a separate array for deserialization
                            byte[] headerBytes = _byteAccumulatorBuffer.GetRange(0, _packetLimits.headerSize).ToArray();

                            // Deserialize the header
                            MuddyPacketHeader pktHeader = _packetSerializer.DeserializeHeader(headerBytes);

                            // Determine the total packet size based on the header information (header + body + tail)
                            int totalPacketSize = _packetLimits.headerSize + (int)pktHeader.BodyLength + _packetLimits.tailSize;

                            // Validate the packet size — binary-flagged packets use the larger binary cap.
                            bool isBinary = (pktHeader.BitFlags & (ushort)MessageFlags.BinaryPayload) != 0;
                            int sizeLimit = isBinary ? _packetLimits.MaxBinaryPacketBytes : _packetLimits.MaxJsonPacketBytes;
                            if (totalPacketSize > sizeLimit) throw new InvalidDataException($"Packet too large. Size: {totalPacketSize}, limit: {sizeLimit}");

                            // Wait until the full packet has accumulated, then extract it.
                            if (_byteAccumulatorBuffer.Count < totalPacketSize) break;

                            // We have a complete packet, so we can deserialize it
                            byte[] fullPacketBytes = _byteAccumulatorBuffer.GetRange(0, totalPacketSize).ToArray();

                            // Remove the processed packet bytes from the accumulator buffer - prevents reprocessing the same bytes in the next loop iteration
                            _byteAccumulatorBuffer.RemoveRange(0, totalPacketSize);

                            MuddyPacket pkt = _packetSerializer.Deserialize(fullPacketBytes);

                            // Create a TransportEnvelope from the deserialized packet
                            var msg = new TransportEnvelope(
                                sessionId: new SessionId(pktHeader.SessionId),
                                messageId: new MessageId(pktHeader.MsgId),
                                messageType: (TransportMessageType)pktHeader.MsgType,
                                flags: (MessageFlags)pkt.Header.BitFlags,
                                payload: pkt.Body,
                                connectionId: ConnId
                            );

                            // Pass the received message to subscribers via the MessageReceived event
                            OnMessageReceived(msg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(ex);
                }
                finally
                {
                    InitiateShutdown(); // signal shared shutdown
                }
            }, ct);
        }

        /// <summary>
        /// Background loop that sends queued protocol envelopes over the socket.
        /// </summary>
        /// <param name="ct">Cancellation token used to stop the send loop.</param>
        /// <returns>A task representing the lifetime of the send loop.</returns>
        /// <remarks>Dequeues envelopes from the send queue, serializes them to wire packets and sends the resulting bytes. Exceptions are reported via the ErrorOccurred event and will trigger shutdown.</remarks>
        private Task SendLoopAsync(CancellationToken ct)
        {
            // The send loop runs in a background task. It processes messages from the sendQue and sends them over the socket.
            // It will continue running until cancellation is requested or an exception occurs. Any exceptions are reported via the ErrorOccurred event.
            return Task.Run(() =>
            {
                try
                {
                    // GetConsumingEnumerable will block until items are available in the queue.
                    // This allows the send loop to efficiently wait for messages to send without busy-waiting.
                    var loopObject = sendQue.GetConsumingEnumerable();

                    // Process each message in the queue and send it over the socket
                    foreach (TransportEnvelope msg in loopObject)
                    {
                        // Optional secondary cancellation guard - to ensure we don't attempt to send messages after cancellation has been requested
                        if (ct.IsCancellationRequested) break;

                        var packet = _packetFactory.CreateMuddyPacket(msg);
                        var bytes = _packetSerializer.Serialize(packet);
                        _acceptedConnection.clientSocket.Send(bytes);
                    }
                }
                catch (Exception ex)
                {
                    // Any failure during send is reported upward
                    OnErrorOccurred(ex);
                }
                finally
                {
                    InitiateShutdown();
                }

            }, ct);
        }

        #endregion

        #region Event invokers for raising events to subscribers

        /// <summary>Invokes the MessageReceived event with the provided protocol envelope.</summary>
        private void OnMessageReceived(TransportEnvelope msg)
        {
            MessageReceived?.Invoke(this, msg);
        }

        /// <summary>Invokes the ConnectionClosed event to signal subscribers that the connection has closed.</summary>
        private void OnConnectionClosed()
        {
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Invokes the ErrorOccurred event to report an error to subscribers.</summary>
        private void OnErrorOccurred(Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }

        #endregion

        #endregion
    }
}
