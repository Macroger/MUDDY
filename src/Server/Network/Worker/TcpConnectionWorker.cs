using Server.Network.Model;
using Server.Network.Packet;
using Shared.Identity;
using Shared.Protocol.Transport;
using Shared.Protocol.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Network.Worker
{
    public class TcpConnectionWorker : IConnectionWorker
    {
        #region Properties and fields

        #region Collection fields for managing send queue and receive buffer

        private BlockingCollection<MessageEnvelope> sendQue = new BlockingCollection<MessageEnvelope>(new ConcurrentQueue<MessageEnvelope>());
        private List<byte> _byteAccumulatorBuffer = new List<byte>();

        #endregion

        #region Core dependencies for managing the connection, packet processing, and serialization

        private AcceptedConnection _acceptedConnection;
        private IPacketFactory _packetFactory;
        private MuddyProtocolLimits _packetLimits;
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

        public ConnectionId ConnId { get; init; }

        public bool IsRunning
        {
            get => _isRunning;
            private set => _isRunning = value;
        }

        #endregion        

        #region Events for message reception, connection closure, and error reporting

        public event EventHandler<MessageEnvelope>? MessageReceived;
        public event EventHandler? ConnectionClosed;
        public event EventHandler<Exception>? ErrorOccurred;

        #endregion

        #endregion

        #region Methods

        #region Constructor to initialize the connection worker with necessary dependencies and configuration
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

        public void Start()
        {
            // If the connection is already running, there's no need to start it again
            if (IsRunning) return;            

            IsRunning = true;

            _sendTask = SendLoopAsync(_ct);
            _receiveTask = ReceiveLoopAsync(_ct);
        }

        public void Stop()
        {
            // If the connection is not running, there's nothing to stop
            if (!IsRunning) return;

            InitiateShutdown();
        }

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

        public bool SendMessage(MessageEnvelope msg)
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

        private Task ReceiveLoopAsync(CancellationToken ct)
        {
            return Task.Run(() =>
            {
                try
                {
                    byte[] tempBuffer = new byte[4096];

                    while (!ct.IsCancellationRequested)
                    {
                        // Read bytes from socket - BLOCKING CALL - will wait until data is available or connection is closed
                        int bytesReceived = _acceptedConnection.clientSocket.Receive(tempBuffer);

                        // If bytesReceived is 0, it means the connection has been gracefully closed by the client
                        if (bytesReceived == 0) break;

                        // Append received bytes to the accumulator buffer
                        _byteAccumulatorBuffer.AddRange(tempBuffer.AsSpan(0, bytesReceived));

                        while(_byteAccumulatorBuffer.Count >= _packetLimits.headerSize )
                        {
                            byte[] headerBytes = _byteAccumulatorBuffer.GetRange(0, _packetLimits.headerSize).ToArray();

                            MuddyPacketHeader pktHeader = _packetSerializer.DeserializeHeader(headerBytes);
                            int totalPacketSize = _packetLimits.headerSize + (int)pktHeader.BodyLength + _packetLimits.tailSize;

                            if( totalPacketSize > _packetLimits.MaxJsonPacketBytes) throw new InvalidDataException($"Packet too large for a JSON packet. Size: {totalPacketSize}");

                            if (_byteAccumulatorBuffer.Count < totalPacketSize) break;

                            // We have a complete packet, so we can deserialize it
                            byte[] fullPacketBytes = _byteAccumulatorBuffer.GetRange(0, totalPacketSize).ToArray();

                            // Remove the processed packet bytes from the accumulator buffer
                            _byteAccumulatorBuffer.RemoveRange(0, totalPacketSize);


                            MuddyPacket pkt = _packetSerializer.Deserialize(fullPacketBytes);

                            var msg = new MessageEnvelope(
                                new MessageId(pktHeader.MsgId),
                                (MessageType)pktHeader.MsgType,
                                (MessageFlags)pkt.Header.BitFlags,
                                pkt.Body
                            );

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

        private Task SendLoopAsync(CancellationToken ct)
        {
            // The send loop runs in a background task, consuming messages from the sendQue and sending them over the socket
            return Task.Run(() => {
                try
                {
                    var loopObject = sendQue.GetConsumingEnumerable();
                    foreach (MessageEnvelope msg in loopObject)
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

        private void OnMessageReceived(MessageEnvelope msg)
        {
            MessageReceived?.Invoke(this, msg);
        }

        private void OnConnectionClosed()
        {
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }

        private void OnErrorOccurred(Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }

        #endregion

        #endregion
    }
}
