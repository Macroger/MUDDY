
using Client.Core.Infrastructure.Events;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static Client.Core.Infrastructure.Events.ClientNetworkEvents.Errors;

namespace Client.GUI
{
    public sealed partial class MainWindow : Window
    {
        private IEventBus _eventBus;
        private bool _isConnected = false;
        private readonly List<string> _commandHistory = new();
        private int _historyIndex = -1;

        private bool _isDragging = false;
        private Windows.Foundation.Point _dragStartPoint;

        private readonly List<ISubscriptionToken> _subscriptions = new();

        public MainWindow(IEventBus eventBus)
        {
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _eventBus = eventBus;
            InitializeComponent();
            this.Closed += MainWindow_Closed;

            SubscribeToEventBus();
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            // Cleanup
            foreach (var subscription in _subscriptions)
            {
                subscription?.Dispose();
            }

            _subscriptions.Clear();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
            string address = ServerAddressBox.Text;
            if (!int.TryParse(ServerPortBox.Text, out int port))
            {
                _eventBus.Publish(
                    EventMessageType.Gui,
                    new ClientGuiEvents.Errors.GuiError("Invalid port number.", null));
                AppendGameOutput("Invalid port number.", "#FFFF6B6B");
                return;
            }

            // Publish connect command - NetworkSupervisor will handle it
            AppendGameOutput($"Connecting to {address}:{port}...", "#FFDAA520");

            _eventBus.Publish(
                EventMessageType.Network,
                new ClientNetworkEvents.Commands.ConnectToServer(address, port));
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isConnected)
            {
                _eventBus.Publish(
                    EventMessageType.Gui,
                    new ClientGuiEvents.Errors.GuiError("Not connected to any server.", null));
                AppendGameOutput("Not connected to any server.", "#FFFF6B6B");
                return;
            }

            // Publish disconnect command
            _eventBus.Publish(
                EventMessageType.Network,
                new ClientNetworkEvents.Commands.DisconnectFromServer());
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
            {
                return;
            }

            if (!_isConnected)
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
                // Publish command to event bus - NetworkSupervisor will handle it
                _eventBus.Publish(
                    EventMessageType.Gui,
                    new ClientGuiEvents.Commands.SendMessageToServer(command));
            }
            catch (Exception ex)
            {
                // Publish error event - will be logged
                _eventBus.Publish(
                    EventMessageType.Gui,
                    new ClientGuiEvents.Errors.GuiError(
                        $"Error sending command: {ex.Message}", ex));
            }

            CommandInputBox.Text = string.Empty;
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

        /// <summary>
        /// Toggles the connection widget dropdown on/off.
        /// </summary>
        private void ToggleConnectionWidget_Click(object sender, RoutedEventArgs e)
        {
            // Toggle visibility of the entire container (backdrop + widget)
            ConnectionWidgetContainer.Visibility =
                ConnectionWidgetContainer.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;

            // Reset position to center when opening
            if (ConnectionWidgetContainer.Visibility == Visibility.Visible)
            {
                // Defer centering until layout is complete
                ConnectionWidgetContainer.Loaded -= OnConnectionWidgetContainerLoaded;
                ConnectionWidgetContainer.Loaded += OnConnectionWidgetContainerLoaded;
            }
        }

        /// <summary>
        /// Placeholder for About dialog.
        /// </summary>
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AppendGameOutput("MUDDY v0.2 - Multi User Dungeon for Dynamic Learning.", "#FFDAA520");
        }

        /// <summary>
        /// Closes the connection widget when clicking the backdrop.
        /// </summary>
        private void ConnectionBackdrop_Click(object sender, PointerRoutedEventArgs e)
        {
            ConnectionWidgetContainer.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Resets the widget to a centered position.
        /// </summary>
        private void ResetWidgetPosition()
        {
            // Center the widget horizontally
            var containerWidth = ConnectionWidgetContainer.ActualWidth;
            var widgetWidth = 420.0; // Match the Border Width
            var leftMargin = (containerWidth - widgetWidth) / 2;

            ConnectionBarOverlay.Margin = new Thickness(leftMargin, 20, 0, 0);
        }

        /// <summary>
        /// Starts dragging the connection widget when the title bar is pressed.
        /// </summary>
        private void TitleBar_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
                _isDragging = true;
                _dragStartPoint = e.GetCurrentPoint(ConnectionWidgetContainer).Position;
                border.CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Moves the connection widget while dragging.
        /// </summary>
        private void TitleBar_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                var currentPoint = e.GetCurrentPoint(ConnectionWidgetContainer).Position;
                var deltaX = currentPoint.X - _dragStartPoint.X;
                var deltaY = currentPoint.Y - _dragStartPoint.Y;

                var currentMargin = ConnectionBarOverlay.Margin;
                var newLeft = currentMargin.Left + deltaX;
                var newTop = currentMargin.Top + deltaY;

                // Constrain to parent container bounds
                var containerWidth = ConnectionWidgetContainer.ActualWidth;
                var containerHeight = ConnectionWidgetContainer.ActualHeight;
                var widgetWidth = 420.0; // Match the Border Width in XAML
                var widgetHeight = 300.0; // Approximate height (adjust if needed)

                // Keep widget within bounds
                newLeft = Math.Max(0, Math.Min(newLeft, containerWidth - widgetWidth));
                newTop = Math.Max(0, Math.Min(newTop, containerHeight - widgetHeight));

                ConnectionBarOverlay.Margin = new Thickness(newLeft, newTop, 0, 0);

                // Update drag start point for next move
                _dragStartPoint = currentPoint;

                e.Handled = true;
            }
        }

        /// <summary>
        /// Stops dragging the connection widget.
        /// </summary>
        private void TitleBar_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                var border = sender as Border;
                border?.ReleasePointerCapture(e.Pointer);
                e.Handled = true;
            }
        }

        private void UpdateConnectionStatus(bool connected)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (connected)
                {
                    // Update title bar status
                    ConnectionStatusIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);
                    ConnectionStatusText.Text = $"Connected: {ServerAddressBox.Text}:{ServerPortBox.Text}";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);

                    // Update button visibility
                    ConnectButton.Visibility = Visibility.Collapsed;
                    DisconnectButton.Visibility = Visibility.Visible;
                }
                else
                {
                    // Update title bar status
                    ConnectionStatusIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    ConnectionStatusText.Text = "Disconnected";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);

                    // Update button visibility
                    ConnectButton.Visibility = Visibility.Visible;
                    DisconnectButton.Visibility = Visibility.Collapsed;
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

        private void SubscribeToEventBus()
        {
            // Subscribe to connection status changes
            _subscriptions.Add(_eventBus.Subscribe<ClientNetworkEvents.Lifecycle.ConnectionStatusChangedEvent>(
                eventType: EventMessageType.Network,
                handler: OnConnectionStatusChanged
            ));

            // Subscribe to network errors so connection failures are visible in the GUI
            _subscriptions.Add(_eventBus.Subscribe<ClientNetworkEvents.Errors.NetworkError>(
                eventType: EventMessageType.Network,
                handler: OnNetworkError
            ));

            // Subscribe to GUI errors
            _subscriptions.Add(_eventBus.Subscribe<ClientGuiEvents.Errors.GuiError>(
                eventType: EventMessageType.Gui,
                handler: OnGuiError
            ));

            // Authentication messages (login success/failure, session established)
            _subscriptions.Add(_eventBus.Subscribe<ClientGuiEvents.Notifications.ReceivedAuthenticationMessage>(
                eventType: EventMessageType.Gui,
                handler: OnAuthenticationMessageReceived
            ));

            // Binary transfer messages (images, files, etc.)
            _subscriptions.Add(_eventBus.Subscribe<ClientGuiEvents.Notifications.ReceivedBinaryTransferMessage>(
                eventType: EventMessageType.Gui,
                handler: OnBinaryTransferMessageReceived
            ));

            // Error messages from server
            _subscriptions.Add(_eventBus.Subscribe<ClientGuiEvents.Notifications.ReceivedErrorMessage>(
                eventType: EventMessageType.Gui,
                handler: OnErrorMessageReceived
            ));

            // Event messages (server-initiated notifications, including chat broadcasts)
            _subscriptions.Add(_eventBus.Subscribe<ClientGuiEvents.Notifications.ReceivedEventMessage>(
                eventType: EventMessageType.Gui,
                handler: OnEventMessageReceived
            ));

            // Response messages (server replies to commands)
            _subscriptions.Add(_eventBus.Subscribe<ClientGuiEvents.Notifications.ReceivedResponseMessage>(
                eventType: EventMessageType.Gui,
                handler: OnResponseMessageReceived
            ));

            // System messages (infrastructure/protocol-level)
            _subscriptions.Add(_eventBus.Subscribe<ClientGuiEvents.Notifications.ReceivedSystemMessage>(
                eventType: EventMessageType.Gui,
                handler: OnSystemMessageReceived
            ));
        }

        /// <summary>
        /// Centers the connection widget after the container has completed layout.
        /// </summary>
        private void OnConnectionWidgetContainerLoaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe to prevent multiple calls
            ConnectionWidgetContainer.Loaded -= OnConnectionWidgetContainerLoaded;
            ResetWidgetPosition();
        }

        private void OnConnectionStatusChanged(ClientNetworkEvents.Lifecycle.ConnectionStatusChangedEvent evnt)
        {
            _isConnected = evnt.ConnectionStatus;
            UpdateConnectionStatus(evnt.ConnectionStatus);

            DispatcherQueue.TryEnqueue(() =>
            {                
                AppendGameOutput(evnt.Message, evnt.ConnectionStatus ? "#FF6BCF7F" : "#FFDAA520");
            });
        }

        private void OnGuiError(ClientGuiEvents.Errors.GuiError evnt)
        {
            // Note: We don't need to append to GUI output here because the caller
            // already does that for immediate user feedback. This subscription exists
            // solely so the logger can capture GUI-level errors.
        }

        private void OnNetworkError(ClientNetworkEvents.Errors.NetworkError evnt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                AppendGameOutput($"[Network Error] {evnt.ErrorMessage}", "#FFFF6B6B");
            });
        }

        private void OnAuthenticationMessageReceived(ClientGuiEvents.Notifications.ReceivedAuthenticationMessage evnt)
        {
            // Marshal to UI thread before touching UI
            DispatcherQueue.TryEnqueue(() =>
            {
                string message = Encoding.UTF8.GetString(evnt.envelope.Payload);
                AppendGameOutput(message, "#FF6BCF7F"); // Green for success
            });
        }

        private void OnBinaryTransferMessageReceived(ClientGuiEvents.Notifications.ReceivedBinaryTransferMessage evnt)
        {
            // Heavy processing on background thread - extract and validate data
            byte[] imageData = evnt.envelope.Payload;
            int dataLength = imageData.Length;

            // Validate before marshalling to UI thread
            if (imageData == null || imageData.Length == 0)
            {
                _eventBus.Publish(
                    EventMessageType.Gui,
                    new ClientGuiEvents.Errors.GuiError("Received empty image data", null));
                DispatcherQueue.TryEnqueue(() =>
                {
                    AppendGameOutput("Error: Received empty image data", "#FFFF6B6B");
                });
                return;
            }

            // Only marshal UI updates to UI thread
            DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    // Clear existing content in MapCanvas
                    MapCanvas.Children.Clear();

                    // Convert byte[] to BitmapImage (must be on UI thread)
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

                    AppendGameOutput($"Image received ({dataLength:N0} bytes)", "#FF88DD88");
                }
                catch (Exception ex)
                {
                    _eventBus.Publish(
                        EventMessageType.Gui,
                        new ClientGuiEvents.Errors.GuiError($"Error displaying image: {ex.Message}", ex));
                    AppendGameOutput($"Error displaying image: {ex.Message}", "#FFFF6B6B");
                }
            });
        }

        private void OnErrorMessageReceived(ClientGuiEvents.Notifications.ReceivedErrorMessage evnt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                string errorMessage = Encoding.UTF8.GetString(evnt.envelope.Payload);
                AppendGameOutput($"Error: {errorMessage}", "#FFFF6B6B"); // Red
            });
        }

        private void OnEventMessageReceived(ClientGuiEvents.Notifications.ReceivedEventMessage evnt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                string eventMessage = Encoding.UTF8.GetString(evnt.envelope.Payload);
                AppendGameOutput(eventMessage, "#FFFFFF"); // White for events
            });
        }

        private void OnResponseMessageReceived(ClientGuiEvents.Notifications.ReceivedResponseMessage evnt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                string response = Encoding.UTF8.GetString(evnt.envelope.Payload);
                AppendGameOutput(response, "#FFD3D3D3"); // Light gray for responses
            });
        }

        private void OnSystemMessageReceived(ClientGuiEvents.Notifications.ReceivedSystemMessage evnt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                string systemMessage = Encoding.UTF8.GetString(evnt.envelope.Payload);
                AppendGameOutput($"[System] {systemMessage}", "#FFDAA520"); // Gold/orange for system
            });
        }


    }
}
