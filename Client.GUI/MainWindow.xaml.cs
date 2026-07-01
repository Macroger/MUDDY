
using ABI.Windows.UI;
using Client.Core.Infrastructure.Events;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using static Client.GUI.App;

namespace Client.GUI
{
    public sealed partial class MainWindow : Window
    {
        private IEventBus _eventBus;
        private bool _isConnected = false;
        private readonly List<string> _commandHistory = new();
        private int _historyIndex = -1;

        private bool _isInitializing;

        private bool _isUserNearBottom = true;

        private double _colorOffset = 0;
        private DispatcherTimer? _colorTimer = null;

        private Compositor _compositor;
        private ContainerVisual _effectsContainer;

        private DispatcherTimer? _backgroundTimer;
        private double _backgroundHue;
        private GradientStop[]? _bgStops;

        private LinearGradientBrush? _borderBrush;
        private double _borderOffset = 0;



        private double[] _bgStopOffsets = new double[] { 0, 90, 180, 270 };

        private DateTime _lastBoundaryBurst = DateTime.MinValue;


        // Track login state
        private bool _isLoggedIn = false;

        private readonly List<OutputLine> _outputLines = new();

        /// <summary>
        /// Gets or sets the login state and triggers panel visibility updates.
        /// </summary>
        private bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                if (_isLoggedIn != value)
                {
                    _isLoggedIn = value;
                    OnLoginStateChanged();
                }
            }
        }

        private readonly List<ISubscriptionToken> _subscriptions = new();

        #region Construction / Startup

        public MainWindow(IEventBus eventBus)
        {
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _eventBus = eventBus;
            InitializeComponent();

            var visual = ElementCompositionPreview.GetElementVisual(this.Content as UIElement);
            _compositor = visual.Compositor;

            // Create container for all effects
            _effectsContainer = _compositor.CreateContainerVisual();
            ElementCompositionPreview.SetElementChildVisual(this.Content as UIElement, _effectsContainer);                        

            // Set minimum window size using OverlappedPresenter
            var presenter = Microsoft.UI.Windowing.OverlappedPresenter.Create();
            presenter.IsResizable = true;
            presenter.IsMinimizable = true;
            presenter.IsMaximizable = true;
            presenter.PreferredMinimumWidth = 1200;
            presenter.PreferredMinimumHeight = 600;

            this.AppWindow.SetPresenter(presenter);

            this.Closed += MainWindow_Closed;

            ThemeManager.ThemeChanged += OnThemeChanged;

            GameOutputScroller.ViewChanged += OnScrollChanged;

            SubscribeToEventBus();

            // Initialize UI controls from saved settings
            InitializeSettingsUI();

            if (ThemeManager.GetCapabilities().EnableTextShimmer)
            {
                StartTextShimmer();
            }
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

            // Authentication messages - session established
            _subscriptions.Add(_eventBus.Subscribe<AuthenticationEvents.Notifications.SessionEstablished>(
                eventType: EventMessageType.Authentication,
                handler: OnSessionEstablished
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
        /// Initializes UI controls to match loaded settings values.
        /// </summary>
        private void InitializeSettingsUI()
        {
            _isInitializing = true;

            // Set toggle switches
            NotificationsToggle.IsOn = App.Settings.NotificationsEnabled;
            AutoConnectToggle.IsOn = App.Settings.AutoConnectOnStartup;
            RememberServerToggle.IsOn = App.Settings.RememberLastServer;

            // Set font size slider
            FontSizeSlider.Value = App.Settings.GameOutputFontSize;
            GameOutputTextBlock.FontSize = App.Settings.GameOutputFontSize;

            // Set theme radio button selection
            AppTheme selectedTheme = App.Settings.SelectedTheme;

            for (int i = 0; i < ThemeRadioButtons.Items.Count; i++)
            {
                if (ThemeRadioButtons.Items[i] is RadioButton radioButton &&
                    radioButton.Tag is string tag &&
                    Enum.TryParse<AppTheme>(tag, out var theme))
                {
                    if (theme == selectedTheme)
                    {
                        ThemeRadioButtons.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Load saved server connection if remember is enabled
            if (App.Settings.RememberLastServer)
            {
                ServerAddressBox.Text = App.Settings.LastServerAddress;
                ServerPortBox.Text = App.Settings.LastServerPort.ToString();
            }

            _isInitializing = false;

        }

        private void InitializeBackgroundAnimation()
        {
            if (!ThemeManager.GetCapabilities().EnableGradientAnimation)
                return;

            if (RootGrid.Background is not LinearGradientBrush brush)
                return;

            _bgStops = brush.GradientStops.ToArray();

            _backgroundTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };


            _backgroundTimer.Tick += (s, e) =>
            {
                if (_bgStops == null) return;

                _backgroundHue += 0.05; // much slower

                for (int i = 0; i < _bgStops.Length; i++)
                {
                    var hue = (_backgroundHue + i * 120) % 360;
                    _bgStops[i].Color = ColorFromHSV(hue, 0.5, 0.8);
                }

            };

            _backgroundTimer.Start();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Placeholder for About dialog.
        /// </summary>
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AppendGameOutput("MUDDY v0.2 - Multi User Dungeon for Dynamic Learning.", "#FFDAA520");
        }

        private void AutoConnectToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggle)
            {
                App.Settings.AutoConnectOnStartup = toggle.IsOn;
                AppSettingsManager.Save(App.Settings);
                AppendGameOutput($"Auto-connect on startup: {(toggle.IsOn ? "Enabled" : "Disabled")}", "#FFDAA520");
            }
        }

        private void Border_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            AppendGameOutput($"Border wheel: {((Border)sender).Name ?? "unnamed"}", "#FFFF6B6B");
            // Don't set e.Handled - let it bubble
        }

        private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                // Store original brush in Tag if not already stored
                if (border.Tag == null)
                {
                    border.Tag = border.BorderBrush;
                }

                // Make it glow with a bright cyan border
                border.BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(
                    0xFF, 0x00, 0xFF, 0xFF)); // Cyan
                border.BorderThickness = new Thickness(4); // Thicker border
            }
        }

        private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border && border.Tag is Brush originalBrush)
            {
                // Restore original border
                border.BorderBrush = originalBrush;
                border.BorderThickness = new Thickness(2); // Original thickness
            }
        }

        private void ClearOutputButton_Click(object sender, RoutedEventArgs e)
        {
            GameOutputTextBlock.Blocks.Clear();
            AppendGameOutput("Output cleared.", "#FFAAAAAA");
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

        private void ConnectionStatus_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderThickness = new Thickness(2);

            }
        }

        private void ConnectionStatus_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderThickness = new Thickness(1);
            }
        }

        private void ConnectionStatus_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                FlyoutBase.ShowAttachedFlyout(element);
            }
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

        private void FontSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (sender is Slider slider && GameOutputTextBlock != null)
            {
                double newSize = slider.Value;
                GameOutputTextBlock.FontSize = newSize;

                App.Settings.GameOutputFontSize = newSize;
                AppSettingsManager.Save(App.Settings);

                AppendGameOutput($"Font size changed to: {newSize:F0}", "#FFDAA520");
            }
        }


        private void GameOutput_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            TriggerMouseBoundaryBurst(e, entering: true);
        }

        private void GameOutput_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            TriggerMouseBoundaryBurst(e, entering: false);
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

        private void NotificationsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isInitializing)
                return;

            if (sender is ToggleSwitch toggle)
            {
                App.Settings.NotificationsEnabled = toggle.IsOn;
                AppSettingsManager.Save(App.Settings);
            }
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

        private void OnConnectionStatusChanged(ClientNetworkEvents.Lifecycle.ConnectionStatusChangedEvent evnt)
        {
            _isConnected = evnt.ConnectionStatus;
            UpdateConnectionStatus(evnt.ConnectionStatus);

            DispatcherQueue.TryEnqueue(() =>
            {
                AppendGameOutput(evnt.Message, evnt.ConnectionStatus ? "#FF6BCF7F" : "#FFDAA520");
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

        private void OnFilterChecked(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton selected)
                return;

            foreach (var child in NearbyFilterPanel.Children)
            {
                if (child is ToggleButton btn && btn != selected)
                    btn.IsChecked = false;
            }
        }

        private void OnGuiError(ClientGuiEvents.Errors.GuiError evnt)
        {
            // Note: We don't need to append to GUI output here because the caller
            // already does that for immediate user feedback. This subscription exists
            // solely so the logger can capture GUI-level errors.
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AppendRenderedLine("Welcome to MUDDY!", "#FFFFFFFF");
            AppendRenderedLine("Connect to the server to begin your adventure...", "#FFFFFFFF");

            InitializeBackgroundAnimation();
        }

        /// <summary>
        /// Handles login state changes by showing/hiding game panels.
        /// </summary>
        private void OnLoginStateChanged()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                // Update UI
            });
        }

        private void OnNetworkError(ClientNetworkEvents.Errors.NetworkError evnt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                AppendGameOutput($"[Network Error] {evnt.ErrorMessage}", "#FFFF6B6B");
            });
        }

        private void OnPanelPointerEntered(object sender, PointerRoutedEventArgs e)
        {

            if (!ThemeManager.GetCapabilities().EnableMouseEffects)
                return;

            var point = e.GetCurrentPoint(this.Content as UIElement).Position;

            SpawnMouseBurst(point);
        }

        private void OnPillButtonHoverEnter(object sender, PointerRoutedEventArgs e)
        {
            if (sender is UIElement el)
                ApplyHoverScale(el, 1.05f);
        }

        private void OnPillButtonHoverExit(object sender, PointerRoutedEventArgs e)
        {
            if (sender is UIElement el)
                ApplyHoverScale(el, 1.0f);
        }

        /// <summary>
        /// Handles session established event by showing game panels.
        /// </summary>
        private void OnSessionEstablished(AuthenticationEvents.Notifications.SessionEstablished evnt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                IsLoggedIn = true;
                AppendGameOutput($"Session established: {evnt.id}", "#FF6BCF7F");
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

        private void OnScrollChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var threshold = 50; // px tolerance

            _isUserNearBottom =
                GameOutputScroller.VerticalOffset >=
                GameOutputScroller.ScrollableHeight - threshold;
        }

        private void OnSystemMessageReceived(ClientGuiEvents.Notifications.ReceivedSystemMessage evnt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                string systemMessage = Encoding.UTF8.GetString(evnt.envelope.Payload);
                AppendGameOutput($"[System] {systemMessage}", "#FFDAA520"); // Gold/orange for system
            });
        }

        private void OnThemeChanged(AppTheme theme)
        {
            var caps = ThemeManager.GetCapabilities();

            if (caps.EnableTextShimmer)
            {
                StartTextShimmer();
            }
                
            else
            {
                StopTextShimmer();
            }

            if (caps.EnableGradientAnimation)
            {
                InitializeBackgroundAnimation();
            }
            else
            {
                StopBackgroundAnimation();
            }


            RebuildOutputText();
        }



        private void RememberServerToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isInitializing)
                return;

            if (sender is ToggleSwitch toggle)
            {
                App.Settings.RememberLastServer = toggle.IsOn;
                AppSettingsManager.Save(App.Settings);
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SendCommandAsync();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // The flyout will open automatically, no action needed here
            // This handler exists in case we want to do something when settings is opened
        }

        private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Validate selection
            if (ThemeRadioButtons.SelectedItem is not RadioButton selected) return;

            if (selected.Tag is string tag &&
                Enum.TryParse<AppTheme>(tag, out var theme))
            {
                ThemeManager.ApplyAndSave(theme);
            }
        }

        private void TriggerMouseBoundaryBurst(PointerRoutedEventArgs e, bool entering)
        {
            if (!ThemeManager.GetCapabilities().EnableMouseEffects)
                return;

            // ✅ prevent rapid double-trigger jitter
            if ((DateTime.Now - _lastBoundaryBurst).TotalMilliseconds < 150)
                return;

            _lastBoundaryBurst = DateTime.Now;

            var pos = e.GetCurrentPoint(GameOutputScroller).Position;

            pos = GameOutputScroller.TransformToVisual(RootGrid).TransformPoint(pos);

            SpawnMouseBurst(pos);

            if (!entering)
            {
                SpawnMouseBurst(pos); // double pulse on exit
            }

        }



        #endregion

        #region Mouse Effects

        private void SpawnMouseBurst(Windows.Foundation.Point position)
        {
            float size = Random.Shared.Next(30, 60);

            var origin = new Vector3(
                (float)position.X - size / 2,
                (float)position.Y - size / 2,
                0
            );

            var color = GetColorForIndex(Random.Shared.Next(0, 360));

            var geometry = _compositor.CreateRoundedRectangleGeometry();
            geometry.Size = new Vector2(size, size);
            geometry.CornerRadius = new Vector2(size / 2, size / 2);

            var layerVisual = _compositor.CreateShapeVisual();
            layerVisual.Size = new Vector2(size, size);
            layerVisual.Offset = origin;

            var ring = _compositor.CreateSpriteShape(geometry);
            ring.StrokeBrush = _compositor.CreateColorBrush(color);
            ring.StrokeThickness = 3f;
            ring.FillBrush = null;

            layerVisual.Shapes.Add(ring);

            _effectsContainer.Children.InsertAtTop(layerVisual);

            // animation
            var scaleAnim = _compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.InsertKeyFrame(0f, new Vector3(0.2f, 0.2f, 1));
            scaleAnim.InsertKeyFrame(1f, new Vector3(2f, 2f, 1));
            scaleAnim.Duration = TimeSpan.FromMilliseconds(600);

            var fadeAnim = _compositor.CreateScalarKeyFrameAnimation();
            fadeAnim.InsertKeyFrame(0f, 1f);
            fadeAnim.InsertKeyFrame(1f, 0f);
            fadeAnim.Duration = TimeSpan.FromMilliseconds(600);

            layerVisual.StartAnimation("Scale", scaleAnim);
            layerVisual.StartAnimation("Opacity", fadeAnim);

            // sparks
            for (int i = 0; i < 6; i++)
            {
                SpawnSpark(origin);
            }

            // cleanup
            _ = Task.Run(async () =>
            {
                await Task.Delay(600);

                _ = DispatcherQueue.TryEnqueue(() =>
                {
                    _effectsContainer.Children.Remove(layerVisual);
                    layerVisual.Dispose();
                });
            });
        }

        private void SpawnSpark(Vector3 origin)
        {
            var spark = _compositor.CreateSpriteVisual();

            float size = Random.Shared.Next(4, 10);

            spark.Size = new Vector2(size, size);
            spark.Offset = origin;

            var color = GetColorForIndex(Random.Shared.Next(0, 360));
            spark.Brush = _compositor.CreateColorBrush(color);

            _effectsContainer.Children.InsertAtTop(spark);

            // random direction
            float angle = (float)(Random.Shared.NextDouble() * Math.PI * 2);
            float distance = Random.Shared.Next(40, 160);

            var dx = MathF.Cos(angle) * distance;
            var dy = MathF.Sin(angle) * distance;

            // move animation
            var offsetAnim = _compositor.CreateVector3KeyFrameAnimation();
            offsetAnim.InsertKeyFrame(1f, origin + new Vector3(dx, dy, 0));
            offsetAnim.Duration = TimeSpan.FromMilliseconds(700);

            // fade animation
            var fadeAnim = _compositor.CreateScalarKeyFrameAnimation();
            fadeAnim.InsertKeyFrame(1f, 0f);
            fadeAnim.Duration = TimeSpan.FromMilliseconds(700);

            spark.StartAnimation("Offset", offsetAnim);
            spark.StartAnimation("Opacity", fadeAnim);

            // cleanup

            _ = Task.Run(async () =>
            {
                await Task.Delay(700);

                _ = DispatcherQueue.TryEnqueue(() =>
                {
                    _effectsContainer.Children.Remove(spark);
                    spark.Dispose();
                });
            });

        }

        #endregion

        #region Shimmer Text

        private Paragraph CreateShimmerText(string text, Windows.UI.Color baseColor)
        {
            var paragraph = new Paragraph();

            int segmentSize = 3; // characters per color block

            for (int i = 0; i < text.Length; i += segmentSize)
            {
                string chunk = text.Substring(i, Math.Min(segmentSize, text.Length - i));

                var run = new Run
                {
                    Text = chunk,
                    Foreground = new SolidColorBrush(GetColorForIndex(i))
                };

                paragraph.Inlines.Add(run);
            }

            return paragraph;
        }

        private void StartTextShimmer()
        {
            if (_colorTimer != null)
                return; // already running

            _colorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(80)
            };

            _colorTimer.Tick += (s, e) =>
            {
                _colorOffset += 2;
                RefreshShimmerText();
            };

            _colorTimer.Start();
        }

        private void StopTextShimmer()
        {
            if (_colorTimer == null)
                return;

            _colorTimer.Stop();
            _colorTimer = null;
        }

        private void RefreshShimmerText()
        {
            if (GameOutputTextBlock.Blocks.Count == 0)
                return;

            var paragraph = GameOutputTextBlock.Blocks[0] as Paragraph;

            if (paragraph == null)
                return;

            int index = 0;

            foreach (var inline in paragraph.Inlines)
            {
                if (inline is Run run)
                {
                    run.Foreground = new SolidColorBrush(GetColorForIndex(index));
                    index += run.Text.Length;
                }
            }
        }


        private Windows.UI.Color GetColorForIndex(int index)
        {
            double hue = (index * 20 + _colorOffset) % 360;

            return ColorFromHSV(hue, 0.8, 1.0);
        }

        #endregion
        

        private void AppendGameOutput(Paragraph paragraph, string? colorHex = null)
        {

        }

        private void AppendGameOutput(string text, string? colorHex = null)
        {
            if(colorHex == null)
            {
                colorHex = "#FFFFFFFF"; // Default to white if no color is provided
            }

            // Store the data first
            _outputLines.Add(new OutputLine
            {
                Text = text,
                ColorHex = colorHex
            });

            // Render based on current theme
            AppendRenderedLine(text, colorHex);


            //if (ThemeManager.GetCapabilities().EnableTextShimmer)
            //{
            //    var shimmerText = CreateShimmerText(text);

            //    DispatcherQueue.TryEnqueue(() =>
            //    {
            //        GameOutputTextBlock.Blocks.Add(shimmerText);

            //        // Auto-scroll to bottom
            //        GameOutputScroller.ChangeView(null, GameOutputScroller.ScrollableHeight, null);
            //    });
            //}
            //else
            //{
            //    var paragraph = new Paragraph();
            //    var run = new Run { Text = text };

            //    if (colorHex == null)
            //    {
            //        run.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
            //    }
            //    else
            //    {
            //        // Parse color
            //        try
            //        {
            //            byte a = byte.Parse(colorHex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
            //            byte r = byte.Parse(colorHex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
            //            byte g = byte.Parse(colorHex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
            //            byte b = byte.Parse(colorHex.Substring(7, 2), System.Globalization.NumberStyles.HexNumber);
            //            run.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
            //        }
            //        catch
            //        {
            //            run.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
            //        }
            //    }

            //    paragraph.Inlines.Add(run);

            //DispatcherQueue.TryEnqueue(() =>
            //{
            //    GameOutputTextBlock.Blocks.Add(paragraph);

            //    // Auto-scroll to bottom
            //    GameOutputScroller.ChangeView(null, GameOutputScroller.ScrollableHeight, null);
            //});
            //}
        }

        private void AppendRenderedLine(string text, string colorHex)
        {

            if (ThemeManager.GetCapabilities().EnableTextShimmer)
            {
                Windows.UI.Color baseColor;

                try
                {
                    byte a = byte.Parse(colorHex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                    byte r = byte.Parse(colorHex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(colorHex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(colorHex.Substring(7, 2), System.Globalization.NumberStyles.HexNumber);

                    baseColor = Windows.UI.Color.FromArgb(a, r, g, b);
                }
                catch
                {
                    baseColor = Microsoft.UI.Colors.White;
                }

                GameOutputTextBlock.Blocks.Add(CreateShimmerText(text, baseColor));
                return;
            }


            var paragraph = new Paragraph();
            var run = new Run { Text = text };

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

            DispatcherQueue.TryEnqueue(() =>
            {
                GameOutputTextBlock.Blocks.Add(paragraph);

                if (_isUserNearBottom)
                {
                    GameOutputScroller.ChangeView(
                        null,
                        GameOutputScroller.ScrollableHeight,
                        null);
                }
            });
            
        }

        private void ApplyHoverScale(UIElement element, float scale)
        {
            element.Scale = new System.Numerics.Vector3(scale, scale, 1f);
        }

        private void ClearAllEffects()
        {
            foreach (var visual in _effectsContainer.Children)
            {
                _effectsContainer.Children.Remove(visual);
                visual.Dispose();
            }
        }

        private Windows.UI.Color ColorFromHSV(double hue, double saturation, double value)
        {
            double c = value * saturation;
            double x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
            double m = value - c;

            double r, g, b;

            if (hue < 60) { r = c; g = x; b = 0; }
            else if (hue < 120) { r = x; g = c; b = 0; }
            else if (hue < 180) { r = 0; g = c; b = x; }
            else if (hue < 240) { r = 0; g = x; b = c; }
            else if (hue < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return Windows.UI.Color.FromArgb(
                255,
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255)
            );
        }

        private (double h, double s, double v) ColorToHSV(Windows.UI.Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0;

            if (delta != 0)
            {
                if (max == r)
                    h = 60 * (((g - b) / delta) % 6);
                else if (max == g)
                    h = 60 * (((b - r) / delta) + 2);
                else
                    h = 60 * (((r - g) / delta) + 4);
            }

            if (h < 0) h += 360;

            double s = max == 0 ? 0 : delta / max;
            double v = max;

            return (h, s, v);
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

        //private void ConnectButton_Click(object sender, RoutedEventArgs e)
        //{
        //    // Validate input
        //    string address = ServerAddressBox.Text;
        //    if (!int.TryParse(ServerPortBox.Text, out int port))
        //    {
        //        _eventBus.Publish(
        //            EventMessageType.Gui,
        //            new ClientGuiEvents.Errors.GuiError("Invalid port number.", null));
        //        AppendGameOutput("Invalid port number.", "#FFFF6B6B");
        //        return;
        //    }

        //    // Publish connect command - NetworkSupervisor will handle it
        //    AppendGameOutput($"Connecting to {address}:{port}...", "#FFDAA520");

        //    _eventBus.Publish(
        //        EventMessageType.Network,
        //        new ClientNetworkEvents.Commands.ConnectToServer(address, port));
        //}        

        private void RebuildOutputText()
        {
            GameOutputTextBlock.Blocks.Clear();

            foreach (var line in _outputLines)
            {
                AppendRenderedLine(line.Text, line.ColorHex);
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

        private void StopBackgroundAnimation()
        {
            _backgroundTimer?.Stop();
            _backgroundTimer = null;
        }

        private void UpdateConnectionStatus(bool connected)
        {
            //DispatcherQueue.TryEnqueue(() =>
            //{
            //    if (connected)
            //    {
            //        // Update title bar status
            //        ConnectionStatusIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);
            //        ConnectionStatusText.Text = $"Connected: {ServerAddressBox.Text}:{ServerPortBox.Text}";
            //        ConnectionStatusText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);

            //        // Update button visibility
            //        ConnectButton.Visibility = Visibility.Collapsed;
            //        DisconnectButton.Visibility = Visibility.Visible;
            //    }
            //    else
            //    {
            //        // Update title bar status
            //        ConnectionStatusIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.Red);
            //        ConnectionStatusText.Text = "Disconnected";
            //        ConnectionStatusText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);

            //        // Update button visibility
            //        ConnectButton.Visibility = Visibility.Visible;
            //        DisconnectButton.Visibility = Visibility.Collapsed;
            //    }
            //});
        }

    }
}
