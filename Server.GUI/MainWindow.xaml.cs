// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Server.Core.Infrastructure.Lifecycle;
using Shared.Domain.Player;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Windows.Graphics;
using static Shared.EventBus.EventTypes.NetworkEvents;

namespace Server.GUI
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly IEventBus _eventBus;
        private ObservableCollection<PlayerDisplay> _activePlayers = new();
        private ObservableCollection<EventEntry> _events = new();
        private DispatcherTimer _timer = new();

        private string _serverStateText = "Running";
        private Brush _serverStateBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);

        private string _listenerStateText = "OFFLINE";
        private Brush _listenerStateBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly List<ISubscriptionToken> _eventSubscriptions = new();

        // Public property for XAML binding
        public ObservableCollection<EventEntry> Events => _events;

        public string ServerStateText
        {
            get => _serverStateText;
            set { _serverStateText = value; OnPropertyChanged(nameof(ServerStateText)); }
        }

        public Brush ServerStateBrush
        {
            get => _serverStateBrush;
            set { _serverStateBrush = value; OnPropertyChanged(nameof(ServerStateBrush)); }
        }

        public string ListenerStateText
        {
            get => _listenerStateText;
            set { _listenerStateText = value; OnPropertyChanged(nameof(ListenerStateText)); }
        }

        public Brush ListenerStateBrush
        {
            get => _listenerStateBrush;
            set { _listenerStateBrush = value; OnPropertyChanged(nameof(ListenerStateBrush)); }
        }


        public MainWindow(IEventBus eventBus)
        {
            try
            {
                InitializeComponent();

                MuteButton.IsEnabled = false;
                KickButton.IsEnabled = false;

                _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

                // Minimal setup - just set data sources and don't subscribe yet
                PlayersListView.ItemsSource = _activePlayers;
                EventLogDataGrid.ItemsSource = _events;

                // Window sizing
                if (AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.PreferredMinimumWidth = 1080;
                    presenter.PreferredMinimumHeight = 1040;
                    presenter.IsResizable = true;
                    presenter.IsMaximizable = true;
                    presenter.IsMinimizable = true;
                }
                this.AppWindow.Resize(new SizeInt32(1280, 1200));

                // Simple UI updates
                PlayerCountText.Text = $"PLAYERS CONNECTED: {_activePlayers.Count}";

                // Timer for server time
                _timer.Interval = TimeSpan.FromSeconds(1);
                _timer.Tick += (s, e) => ServerTimeText.Text = $"CURRENT TIME: {DateTime.Now:HH:mm:ss tt}";
                _timer.Start();

                // Uptime timer
                var startTime = DateTime.Now;
                var uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                uptimeTimer.Tick += (s, e) =>
                {
                    var uptime = DateTime.Now - startTime;
                    UptimeText.Text = uptime.ToString(@"hh\:mm\:ss");
                };
                uptimeTimer.Start();

                // Subscribe to server state changes
                try
                {
                    // Subscribe to specific event types for WinUI safety
                    _eventSubscriptions.Add(_eventBus.Subscribe<ServerStateChangedEvent>(EventMessageType.System, OnServerStateChanged));
                    _eventSubscriptions.Add(_eventBus.Subscribe<ServerStateChangeRequestedEvent>(EventMessageType.System, OnServerStateChangeRequested));
                    _eventSubscriptions.Add(_eventBus.Subscribe<ListenerStateChanged>(EventMessageType.Network, OnListenerStateChanged));

                    // Subscribe to player events
                    _eventSubscriptions.Add(_eventBus.Subscribe<PlayerEvents.PlayerEnteredWorldEvent>(EventMessageType.World, OnPlayerEnteredWorld));
                    _eventSubscriptions.Add(_eventBus.Subscribe<PlayerEvents.PlayerLeftWorldEvent>(EventMessageType.Domain, OnPlayerLeftWorld));

                    // Subscribe to EventEnvelope for different categories - these are safe because we control the payload
                    _eventSubscriptions.Add(_eventBus.Subscribe<EventEnvelope>(EventMessageType.System, OnSystemEventReceived));
                    _eventSubscriptions.Add(_eventBus.Subscribe<EventEnvelope>(EventMessageType.Network, OnNetworkEventReceived));
                    _eventSubscriptions.Add(_eventBus.Subscribe<EventEnvelope>(EventMessageType.Authentication, OnAuthEventReceived));
                    _eventSubscriptions.Add(_eventBus.Subscribe<EventEnvelope>(EventMessageType.Error, OnErrorEventReceived));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Subscription failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CRITICAL ERROR in MainWindow initialization: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private void OnServerStateChangeRequested(ServerStateChangeRequestedEvent evt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _events.Insert(0, ToEventEntry(DateTime.Now, "System", $"State change requested: {evt.RequestedState}"));
                if (_events.Count > 100) _events.RemoveAt(_events.Count - 1);
            });
        }

        private void OnSystemEventReceived(EventEnvelope envelope)
        {
            // Avoid pattern matching - it can trigger reflection in WinUI
            if (envelope.Payload == null) return;

            try
            {
                var reason = (EventReason)envelope.Payload;
                DispatcherQueue.TryEnqueue(() =>
                {
                    _events.Insert(0, ToEventEntry(DateTime.Now, "System", reason.Message));
                    if (_events.Count > 100) _events.RemoveAt(_events.Count - 1);
                });
            }
            catch (InvalidCastException)
            {
                // Not an EventReason, ignore
            }
        }

        private void OnNetworkEventReceived(EventEnvelope envelope)
        {
            // Avoid pattern matching - it can trigger reflection in WinUI
            if (envelope.Payload == null) return;

            try
            {
                var reason = (EventReason)envelope.Payload;
                DispatcherQueue.TryEnqueue(() =>
                {
                    _events.Insert(0, ToEventEntry(DateTime.Now, "Network", reason.Message));
                    if (_events.Count > 100) _events.RemoveAt(_events.Count - 1);
                });
            }
            catch (InvalidCastException)
            {
                // Not an EventReason, ignore
            }
        }

        private void OnAuthEventReceived(EventEnvelope envelope)
        {
            // Avoid pattern matching - it can trigger reflection in WinUI
            if (envelope.Payload == null) return;

            try
            {
                var reason = (EventReason)envelope.Payload;
                DispatcherQueue.TryEnqueue(() =>
                {
                    _events.Insert(0, ToEventEntry(DateTime.Now, "Auth", reason.Message));
                    if (_events.Count > 100) _events.RemoveAt(_events.Count - 1);
                });
            }
            catch (InvalidCastException)
            {
                // Not an EventReason, ignore
            }
        }

        private void OnErrorEventReceived(EventEnvelope envelope)
        {
            // Avoid pattern matching - it can trigger reflection in WinUI
            if (envelope.Payload == null) return;

            try
            {
                var reason = (EventReason)envelope.Payload;
                DispatcherQueue.TryEnqueue(() =>
                {
                    _events.Insert(0, ToEventEntry(DateTime.Now, "Error", reason.Message));
                    if (_events.Count > 100) _events.RemoveAt(_events.Count - 1);
                });
            }
            catch (InvalidCastException)
            {
                // Not an EventReason, ignore
            }
        }

        private void ClearEventLog_Click(object sender, RoutedEventArgs e)
        {
            if (EventLogDataGrid.ItemsSource is ObservableCollection<EventEntry> eventLog)
            {
                eventLog.Clear();
            }
        }

        public static PlayerDisplay ToPlayerDisplay(PlayerState state)
        {
            return new PlayerDisplay
            {
                Name = state.PlayerName,
                Location = state.CurrentLocation.ToString() ?? string.Empty
            };
        }

        private void OnAnyEventReceived(object evt)
        {
            try
            {
                // Skip EventEnvelope entirely - they're handled by dedicated typed subscribers and cause WinUI reflection crashes
                if (evt is EventEnvelope)
                {
                    return;
                }

                // Skip Player/World events - they're handled by dedicated typed handlers and cause WinUI reflection issues
                if (evt is PlayerEvents.PlayerEnteredWorldEvent or PlayerEvents.PlayerLeftWorldEvent)
                {
                    return;
                }

                string type = evt.GetType().Name;
                string message = string.Empty;

                switch (evt)
                {
                    case EventReason reason:
                        message = FormatEventReason(reason);
                        break;
                    case ServerStateChangedEvent stateChanged:
                        message = $"State changed from {stateChanged.PreviousState} to {stateChanged.NewState}";
                        break;
                    case ServerStateChangeRequestedEvent stateRequest:
                        message = $"State change requested: {stateRequest.RequestedState}";
                        break;
                    case EventEnvelope envelopeEvent:
                        if (envelopeEvent.Payload is EventReason envelopeReason)
                        {
                            message = FormatEventReason(envelopeReason);
                        }
                        else
                        {
                            message = $"Envelope: {envelopeEvent.Payload?.GetType().Name ?? "null"}";
                        }
                        break;
                    default:
                        try
                        {
                            message = evt.ToString() ?? "(null event)";
                        }
                        catch
                        {
                            message = $"Event of type {type} (ToString failed)";
                        }
                        break;
                }

                DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        _events.Insert(0, ToEventEntry(DateTime.Now, type, message));
                        if (_events.Count > 100) _events.RemoveAt(_events.Count - 1);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] ERROR in event log update: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] ERROR in OnAnyEventReceived: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Stack trace: {ex.StackTrace}");
            }
        }

        private string FormatEventReason(EventReason reason)
        {
            // TEMPORARY: Ultra-simple version - no reflection, no fancy formatting
            // Just return the message. We'll add back the Data formatting later once stable.
            return reason.Message;
        }

        private string GetIdentityValue(object identityObject)
        {
            // Try to get the Value property from identity types
            try
            {
                var prop = identityObject.GetType().GetProperty("Value");
                if (prop != null)
                {
                    var value = prop.GetValue(identityObject);
                    return value?.ToString() ?? "null";
                }
            }
            catch { }

            return identityObject.ToString() ?? string.Empty;
        }

        public static EventEntry ToEventEntry(DateTime time, string source, string message)
        {
            return new EventEntry
            {
                Time = time.ToString("HH:mm:ss"),
                Source = source,
                Message = message
            };
        }

        private void OnServerStateChanged(ServerStateChangedEvent args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                switch (args.NewState)
                {
                    case ServerStateEnum.ACTIVE:
                        StatusTextBlock.Text = "STATUS: ACTIVE";
                        StatusTextBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                        break;
                    case ServerStateEnum.MAINTENANCE:
                        StatusTextBlock.Text = "STATUS: MAINTENANCE";
                        StatusTextBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 165, 0));
                        break;
                    case ServerStateEnum.SHUTTING_DOWN:
                        StatusTextBlock.Text = "STATUS: SHUTTING_DOWN";
                        StatusTextBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 50, 50));
                        break;
                    case ServerStateEnum.LOADING:
                        StatusTextBlock.Text = "STATUS: LOADING";
                        StatusTextBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 170, 170, 170));
                        break;
                }

                ServerStateText = args.NewState.ToString();
                ServerStateBrush = args.NewState switch
                {
                    ServerStateEnum.ACTIVE => new SolidColorBrush(Microsoft.UI.Colors.Green),
                    ServerStateEnum.SHUTTING_DOWN => new SolidColorBrush(Microsoft.UI.Colors.Red),
                    ServerStateEnum.MAINTENANCE => new SolidColorBrush(Microsoft.UI.Colors.Orange),
                    _ => new SolidColorBrush(Microsoft.UI.Colors.Gray)
                };
            });
        }

        private void OnListenerStateChanged(ListenerStateChanged evnt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (evnt.IsListenerStarted == true)
                {
                    _listenerStateText = "ONLINE";
                    ListenerStateText = _listenerStateText;
                    ListenerStateBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
                }
                else
                {
                    _listenerStateText = "OFFLINE";
                    ListenerStateText = _listenerStateText;
                    ListenerStateBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
                }

                ToggleListenerButton.IsEnabled = true; // Re-enable the button after state update

            });
        }

        private void OnPlayerEnteredWorld(PlayerEvents.PlayerEnteredWorldEvent evt)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] OnPlayerEnteredWorld called: Player={evt.PlayerName}, Room={evt.StartingRoom.Value}");

                DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] Inside dispatcher queue for player {evt.PlayerName}");

                        // Add player to the active players list
                        var playerDisplay = new PlayerDisplay
                        {
                            Name = evt.PlayerName,
                            Location = evt.StartingRoom.Value
                        };
                        _activePlayers.Add(playerDisplay);

                        System.Diagnostics.Debug.WriteLine($"[MainWindow] Player added to list. Count: {_activePlayers.Count}");

                        // Update player count
                        PlayerCountText.Text = $"PLAYERS CONNECTED: {_activePlayers.Count}";

                        // Log to event display
                        _events.Insert(0, ToEventEntry(DateTime.Now, "PlayerEvents", $"Player {evt.PlayerName} entered at {evt.StartingRoom.Value}"));
                        if (_events.Count > 100) _events.RemoveAt(_events.Count - 1);

                        System.Diagnostics.Debug.WriteLine($"[MainWindow] PlayerCountText updated successfully");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] ERROR in dispatcher queue: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] Stack trace: {ex.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] ERROR in OnPlayerEnteredWorld: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Stack trace: {ex.StackTrace}");
            }
        }

        private void OnPlayerLeftWorld(PlayerEvents.PlayerLeftWorldEvent evt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                // Find and remove the player by name
                var playerToRemove = _activePlayers.FirstOrDefault(p => p.Name == evt.PlayerName);
                if (playerToRemove != null)
                {
                    _activePlayers.Remove(playerToRemove);
                }

                // Update player count
                PlayerCountText.Text = $"PLAYERS CONNECTED: {_activePlayers.Count}";

                // Log to event display
                _events.Insert(0, ToEventEntry(DateTime.Now, "PlayerEvents", $"Player {evt.PlayerName} left from {evt.LastRoom.Value}"));
                if (_events.Count > 100) _events.RemoveAt(_events.Count - 1);
            });
        }

        private void MutePlayer_Click(object sender, RoutedEventArgs e)
        {
            var selectedPlayer = PlayersListView.SelectedItem as PlayerDisplay;
            if (selectedPlayer != null)
            {
                // Fire a request to have the player muted/unmuted
            }
        }

        private void KickPlayer_Click(object sender, RoutedEventArgs e)
        {
            var selectedPlayer = PlayersListView.SelectedItem as PlayerDisplay;
            if (selectedPlayer != null)
            {
                // Kick logic here
                // Example: _eventBus.Publish(...);
            }
        }

        private void PlayersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = PlayersListView.SelectedItem != null;
            MuteButton.IsEnabled = hasSelection;
            KickButton.IsEnabled = hasSelection;
        }

        private void ToggleListenerButton_Click(object sender, RoutedEventArgs e)
        {
            // Check current state and publish appropriate event to toggle listener
            if (ListenerStateText == "ONLINE")
            {
                // Publish event to stop listener
                _eventBus.Publish(EventMessageType.Network, new StopListenerRequest());
            }
            else
            {
                // Publish event to start listener
                _eventBus.Publish(EventMessageType.Network, new StartListnerRequest());
            }

            ToggleListenerButton.IsEnabled = false; // Disable button until we get confirmation of state change
        }

        private void SetActiveButton_Click(object sender, RoutedEventArgs e)
        {
            _eventBus.Publish(EventMessageType.System, new ServerStateChangeRequestedEvent(ServerStateEnum.ACTIVE));
        }

        private void SetMaintenanceButton_Click(object sender, RoutedEventArgs e)
        {
            _eventBus.Publish(EventMessageType.System, new ServerStateChangeRequestedEvent(ServerStateEnum.MAINTENANCE));
        }

        private void SetShutdownButton_Click(object sender, RoutedEventArgs e)
        {
            _eventBus.Publish(EventMessageType.System, new ServerStateChangeRequestedEvent(ServerStateEnum.SHUTTING_DOWN));
        }
    }

    public class PlayerDisplay
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Ping { get; set; } = "0ms";

        public bool IsMuted { get; set; }
    }

    public class EventEntry
    {
        public string Time { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "Info";
    }

    public class SeverityColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value?.ToString() switch
            {
                "Warning" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 165, 0)),
                "Error" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 50, 50)),
                "Info" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)),
                _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 170, 170, 170))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}