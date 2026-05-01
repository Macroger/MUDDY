using Client.Core.CommandPipeline;
using Client.Core.Network;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Shared.EventBus;
using Shared.Identity;
using Shared.Logging;
using Shared.Protocol.Transport;
using Shared.Protocol.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Client.GUI
{
    public sealed partial class MainWindow : Window
    {
        private ClientNetworkService? _networkService;
        private ClientCommandPipelineOrchestrator? _orchestrator;
        private IEventBus? _eventBus;
        private PacketLogger? _packetLogger;
        private bool _isConnected = false;
        private readonly List<string> _commandHistory = new();
        private int _historyIndex = -1;

        public MainWindow()
        {
            InitializeComponent();
            this.Closed += MainWindow_Closed;

            // Subscribe to handlers
            ChatMessageHandler.OnChatMessageReceived += OnChatMessageReceived;
            EventMessageHandler.OnEventReceived += OnEventReceived;
            BinaryTransferHandler.OnImageReceived += OnImageReceived;
            ErrorMessageHandler.OnErrorReceived += OnErrorReceived;
            ResponseMessageHandler.OnResponseReceived += OnResponseReceived;
            AuthSuccessMessageHandler.OnAuthSuccessReceived += OnAuthSuccessReceived;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            // Cleanup
            ChatMessageHandler.OnChatMessageReceived -= OnChatMessageReceived;
            EventMessageHandler.OnEventReceived -= OnEventReceived;
            BinaryTransferHandler.OnImageReceived -= OnImageReceived;
            ErrorMessageHandler.OnErrorReceived -= OnErrorReceived;
            ResponseMessageHandler.OnResponseReceived -= OnResponseReceived;
            AuthSuccessMessageHandler.OnAuthSuccessReceived -= OnAuthSuccessReceived;
            _packetLogger?.Dispose();
            _networkService?.DisconnectAsync().Wait();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected)
            {
                AppendGameOutput("Already connected to server.", "#FFFF6B6B");
                return;
            }

            try
            {
                // Initialize services
                _eventBus = new BasicEventBus();
                var protocolLimits = new MuddyProtocolLimits();
                var packetSerializer = new MuddyPacketSerializer(protocolLimits);
                var packetFactory = new MuddyPacketFactory();

                // Initialize packet logging to file using safe path helper
                string logFilePath = LogPathHelper.CreateTimestampedLogPath("client_packets");
                var packetLogFileWriter = new StandardLogFileWriter(logFilePath, append: true);
                _packetLogger = new PacketLogger(_eventBus, packetLogFileWriter);

                // Log to user where logs are being saved
                AppendGameOutput($"Packet logs will be saved to: {LogPathHelper.GetLogDirectory()}", "#FF808080");

                _networkService = new ClientNetworkService(_eventBus, packetSerializer, packetFactory, protocolLimits);
                _orchestrator = new ClientCommandPipelineOrchestrator();

                // Register handlers for all message types
                _orchestrator.RegisterHandler("Chat", new ChatMessageHandler());
                _orchestrator.RegisterHandler("Event", new EventMessageHandler());
                _orchestrator.RegisterHandler("BinaryTransfer", new BinaryTransferHandler());
                _orchestrator.RegisterHandler("Error", new ErrorMessageHandler());
                _orchestrator.RegisterHandler("Response", new ResponseMessageHandler());
                _orchestrator.RegisterHandler("AuthSuccess", new AuthSuccessMessageHandler());

                // Update endpoint
                string address = ServerAddressBox.Text;
                if (!int.TryParse(ServerPortBox.Text, out int port))
                {
                    AppendGameOutput("Invalid port number.", "#FFFF6B6B");
                    return;
                }

                _networkService.UpdateEndpoint(address, port);

                // Subscribe to network events
                _networkService.OnSessionEstablished += OnSessionEstablished;

                // Subscribe to packet log events to forward to orchestrator
                var subscriptionToken = _eventBus.Subscribe<EventEnvelope>(EventMessageType.PacketLog, OnPacketReceived);

                // Connect
                AppendGameOutput($"Connecting to {address}:{port}...", "#FFDAA520");
                await _networkService.ConnectAsync();

                _isConnected = true;
                UpdateConnectionStatus(true);
                AppendGameOutput("Connected successfully!", "#FF6BCF7F");

                // Start the orchestrator
                _orchestrator.Start();
            }
            catch (Exception ex)
            {
                AppendGameOutput($"Connection failed: {ex.Message}", "#FFFF6B6B");
                _isConnected = false;
                UpdateConnectionStatus(false);
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isConnected || _networkService == null)
            {
                AppendGameOutput("Not connected to any server.", "#FFFF6B6B");
                return;
            }

            try
            {
                await _networkService.DisconnectAsync();
                // _orchestrator doesn't have a Stop method, so we'll just stop using it
                _isConnected = false;
                UpdateConnectionStatus(false);
                AppendGameOutput("Disconnected from server.", "#FFDAA520");
            }
            catch (Exception ex)
            {
                AppendGameOutput($"Error during disconnect: {ex.Message}", "#FFFF6B6B");
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SendCommandAsync();
        }

        private void CommandInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _ = SendCommandAsync();
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Up)
            {
                NavigateCommandHistory(-1);
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Down)
            {
                NavigateCommandHistory(1);
                e.Handled = true;
            }
        }

        private async Task SendCommandAsync()
        {
            string command = CommandInputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(command))
                return;

            if (!_isConnected || _networkService == null)
            {
                AppendGameOutput("Not connected to server. Please connect first.", "#FFFF6B6B");
                return;
            }

            // Add to history
            _commandHistory.Add(command);
            _historyIndex = _commandHistory.Count;

            // Echo command
            AppendGameOutput($"> {command}", "#FF6495ED");

            try
            {
                // Send command to server
                await SendCommandToServerAsync(command);
            }
            catch (Exception ex)
            {
                AppendGameOutput($"Error sending command: {ex.Message}", "#FFFF6B6B");
            }

            CommandInputBox.Text = string.Empty;
        }

        private async Task SendCommandToServerAsync(string command)
        {
            if (_networkService == null)
                throw new InvalidOperationException("Network service not initialized");

            // Parse the command into verb and arguments
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;

            string verb = parts[0];
            string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            // Debug output
            System.Diagnostics.Debug.WriteLine($"Sending command - Verb: '{verb}', Args: [{string.Join(", ", args.Select(a => $"'{a}'"))}]");

            // Create JSON command object
            var jsonCommand = new
            {
                verb = verb,
                args = args
            };

            // Serialize to JSON
            string json = JsonSerializer.Serialize(jsonCommand);

            var payload = System.Text.Encoding.UTF8.GetBytes(json);

            // Generate a random message ID
            var random = new Random();
            var messageId = new MessageId((uint)random.Next());

            var envelope = new TransportEnvelope(
                messageId: messageId,
                messageType: TransportMessageType.Command,
                flags: MessageFlags.None,
                payload: payload,
                connectionId: new ConnectionId("client"),
                sessionId: _networkService.SessionId
            );

            await _networkService.SendMessageAsync(envelope);
        }

        private void NavigateCommandHistory(int direction)
        {
            if (_commandHistory.Count == 0)
                return;

            _historyIndex += direction;

            if (_historyIndex < 0)
                _historyIndex = 0;
            else if (_historyIndex >= _commandHistory.Count)
            {
                _historyIndex = _commandHistory.Count;
                CommandInputBox.Text = string.Empty;
                return;
            }

            CommandInputBox.Text = _commandHistory[_historyIndex];
        }

        private void QuickCommand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string command)
            {
                CommandInputBox.Text = command;
                _ = SendCommandAsync();
            }
        }

        private void MovementButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string direction)
            {
                CommandInputBox.Text = direction;
                _ = SendCommandAsync();
            }
        }

        private void ClearOutputButton_Click(object sender, RoutedEventArgs e)
        {
            GameOutputTextBlock.Blocks.Clear();
            AppendGameOutput("Output cleared.", "#FFAAAAAA");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            AppendGameOutput("Settings panel not yet implemented.", "#FFAAAAAA");
        }

        private void OnSessionEstablished(SessionId sessionId)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                AppendGameOutput($"Session established: {sessionId}", "#FF6BCF7F");
            });
        }

        private void OnChatMessageReceived(string message)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                AppendGameOutput(message, "#FFFFDD88");
            });
        }

        private void OnEventReceived(string eventMessage)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                AppendGameOutput(eventMessage, "#FFE0E0E0");
            });
        }

        private void OnErrorReceived(string errorMessage)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                AppendGameOutput($"ERROR: {errorMessage}", "#FFFF6B6B");
            });
        }

        private void OnResponseReceived(string responseMessage)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                AppendGameOutput(responseMessage, "#FF88DD88");
            });
        }

        private void OnAuthSuccessReceived(string authMessage)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                AppendGameOutput(authMessage, "#FF6BCF7F");
            });
        }

        private void OnImageReceived(byte[] imageData)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    // Clear existing content in MapCanvas
                    MapCanvas.Children.Clear();

                    // Convert byte[] to BitmapImage
                    using var stream = new MemoryStream(imageData);
                    var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                    await bitmap.SetSourceAsync(stream.AsRandomAccessStream());

                    // Create Image control to display the bitmap
                    var image = new Image
                    {
                        Source = bitmap,
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                        Width = MapCanvas.ActualWidth > 0 ? MapCanvas.ActualWidth : 280,
                        Height = MapCanvas.ActualHeight > 0 ? MapCanvas.ActualHeight : 200
                    };

                    // Add to canvas
                    MapCanvas.Children.Add(image);

                    AppendGameOutput($"Image received ({imageData.Length:N0} bytes)", "#FF88DD88");
                }
                catch (Exception ex)
                {
                    AppendGameOutput($"Error displaying image: {ex.Message}", "#FFFF6B6B");
                }
            });
        }

        private async Task OnPacketReceivedAsync(EventReason reason)
        {
            // The Data field is an anonymous object with properties
            if (reason.Data != null)
            {
                var dataType = reason.Data.GetType();

                // Get Direction property
                var directionProp = dataType.GetProperty("Direction");
                var direction = directionProp?.GetValue(reason.Data)?.ToString();

                if (direction == "Inbound")
                {
                    // Get Envelope property
                    var envelopeProp = dataType.GetProperty("Envelope");
                    var envelope = envelopeProp?.GetValue(reason.Data) as TransportEnvelope;

                    if (envelope != null)
                    {
                        // Forward to orchestrator
                        _orchestrator?.ProcessMessage(envelope);
                    }
                }
            }
        }

        private void OnPacketReceived(EventEnvelope envelope)
        {
            if (envelope.Payload is EventReason reason)
            {
                _ = OnPacketReceivedAsync(reason);
            }
        }

        private void UpdateConnectionStatus(bool connected)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (connected)
                {
                    ConnectionStatusText.Text = "Connected";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);
                    ConnectButton.IsEnabled = false;
                }
                else
                {
                    ConnectionStatusText.Text = "Disconnected";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
                    ConnectButton.IsEnabled = true;
                }
            });
        }

        private void AppendGameOutput(string text, string colorHex)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                var paragraph = new Paragraph();
                var run = new Run { Text = text };

                // Parse color
                try
                {
                    byte a = byte.Parse(colorHex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                    byte r = byte.Parse(colorHex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(colorHex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(colorHex.Substring(7, 2), System.Globalization.NumberStyles.HexNumber);
                    run.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
                }
                catch
                {
                    run.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                }

                paragraph.Inlines.Add(run);
                GameOutputTextBlock.Blocks.Add(paragraph);

                // Auto-scroll to bottom
                GameOutputScroller.ChangeView(null, GameOutputScroller.ScrollableHeight, null);
            });
        }
    }
}
