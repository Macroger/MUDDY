// =============================================================================
/// @file       MainWindow.xaml.cs
/// @namespace  Server.GUI
/// @brief      Main window UI for the MUDDY server administration interface.
/// @details    Displays server state, active players, network listener status,
///             and event logs. Subscribes to strongly-typed events from the
///             event bus and marshals all UI updates to the dispatcher thread.
///             Uses WinUI 3 for the user interface.
// =============================================================================
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Server.Core.Infrastructure.Events;
using Server.Core.Infrastructure.Lifecycle;
using Server.Core.Infrastructure.Metrics;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using Windows.Graphics;

namespace Server.GUI
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        #region Private fields and constants

        /// <summary>
        /// Maximum number of entries to keep in the event log before removing the oldest.
        /// Prevents unbounded memory growth while maintaining recent history.
        /// </summary>
        private const int MaxEventLogEntries = 100;

        private DispatcherTimer _timer = new();
        private DispatcherTimer _uptimeTimer = new();
        private DateTime? _serverStartTime = null;

        #endregion

        #region Collections and lists

        private readonly ObservableCollection<PlayerDisplay> _activePlayers = new();
        private readonly ObservableCollection<EventEntry> _events = new();

        /// <summary>
        /// Observable collection of event log entries for XAML data binding.
        /// Displayed in the event log grid in the UI.
        /// </summary>
        public ObservableCollection<EventEntry> Events => _events;

        public ObservableCollection<MetricSample> MetricSamples { get; private set; } = new();

        private readonly List<ISubscriptionToken> _subscriptions = new();

        #endregion

        #region Dependency Injection

        private readonly IEventBus _eventBus;

        #endregion

        #region Server state

        private ServerStateEnum _serverState = ServerStateEnum.LOADING;

        private string _serverStateText = "Running";
        private Brush _serverStateBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);

        private string _listenerStateText = "OFFLINE";
        private Brush _listenerStateBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets the server state text displayed in the UI.
        /// Notifies the UI when the value changes.
        /// </summary>
        public string ServerStateText
        {
            get => _serverStateText;
            set
            {
                _serverStateText = value;
                OnPropertyChanged(nameof(ServerStateText));
            }
        }

        /// <summary>
        /// Gets or sets the brush used to color the server state indicator in the UI.
        /// Changes based on the current server state (green for active, red for shutdown, etc.).
        /// </summary>
        public Brush ServerStateBrush
        {
            get => _serverStateBrush;
            set { _serverStateBrush = value; OnPropertyChanged(nameof(ServerStateBrush)); }
        }

        /// <summary>
        /// Gets or sets the listener state text displayed in the UI ("ONLINE" or "OFFLINE").
        /// Notifies the UI when the value changes.
        /// </summary>
        public string ListenerStateText
        {
            get => _listenerStateText;
            set
            {
                _listenerStateText = value;
                OnPropertyChanged(nameof(ListenerStateText));
            }
        }

        /// <summary>
        /// Gets or sets the brush used to color the listener state indicator in the UI.
        /// Green when online, red when offline.
        /// </summary>
        public Brush ListenerStateBrush
        {
            get => _listenerStateBrush;
            set
            {
                _listenerStateBrush = value;
                OnPropertyChanged(nameof(ListenerStateBrush));
            }
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion

        #region Constructor

        public MainWindow(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            InitializeComponent();

            this.Closed += OnWindowClosed;

            // Window sizing
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.PreferredMinimumWidth = 1080;
                presenter.PreferredMinimumHeight = 1040;
                presenter.IsResizable = true;
                presenter.IsMaximizable = true;
                presenter.IsMinimizable = true;
            }

            this.AppWindow.Resize(new SizeInt32(1280, 1070));

            MuteButton.IsEnabled = false;
            KickButton.IsEnabled = false;

            // Setup the data sources for Players list and the Event log.
            PlayersListView.ItemsSource = _activePlayers;
            EventLogDataGrid.ItemsSource = _events;

            // Set initial player count (should be 0)
            PlayerCountText.Text = $"PLAYERS CONNECTED: {_activePlayers.Count}";

            _serverStartTime = DateTime.Now;

            // Timer for server time
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnServerTimerTick;
            _timer.Start();

            // Uptime timer
            _uptimeTimer.Interval = TimeSpan.FromSeconds(1);
            _uptimeTimer.Tick += OnUptimeTimerTick;            
            _uptimeTimer.Start();            

            // Subscribe to double click event on the event log rows to show details (if needed)
            EventLogDataGrid.DoubleTapped += OnEventLogRowDoubleTapped;

            // Subscribe to server state changes                
            SubscribeToEvents();
        }

        #endregion

        #region Event handlers for system events

        private void OnListenerStateChanged(NetworkEvents.Lifecycle.ListenerStateChanged evnt)
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

        private void OnMetricCollected(MetricsEvents.Notifications.MetricSampleCollected evt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                MetricSamples.Add(evt.Sample);

                // Keep recent 3600 samples (1 hour at 1-sec interval)
                while (MetricSamples.Count > 3600)
                {
                    MetricSamples.RemoveAt(0);
                }
            });
        }

        private void OnPlayerEnteredWorld(WorldEvents.Notifications.PlayerEnteredWorldEvent evt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                PlayerDisplay playerDisplay = new PlayerDisplay
                {
                    Name = evt.PlayerName,
                    Location = evt.StartingRoom.Value
                };

                _activePlayers.Add(playerDisplay);
                PlayerCountText.Text = $"PLAYERS CONNECTED: {_activePlayers.Count}";
            });
        }

        private void OnPlayerLeftWorld(WorldEvents.Notifications.PlayerLeftWorldEvent evt)
        {            
            DispatcherQueue.TryEnqueue(() =>
            {
                // Find and remove the player by name
                PlayerDisplay? playerToRemove = _activePlayers.FirstOrDefault(p => p.Name == evt.PlayerName);

                // If found, remove from the list
                if (playerToRemove != null) _activePlayers.Remove(playerToRemove);

                // Update player count
                PlayerCountText.Text = $"PLAYERS CONNECTED: {_activePlayers.Count}";
            });
        }

        private void OnServerStateChanged(SystemEvents.Lifecycle.ServerStateChangedEvent args)
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

                _serverState = args.NewState;
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

        private void OnServerTimerTick(object? sender, object e)
        {
            ServerTimeText.Text = $"CURRENT TIME: {DateTime.Now:HH:mm:ss}";
        }

        private void OnUptimeTimerTick(object? sender, object e)
        {
            if (_serverStartTime is null)
            {
                return;
            }

            TimeSpan uptime = DateTime.Now - _serverStartTime.Value;
            UptimeText.Text = uptime.ToString(@"hh\:mm\:ss");
        }

        /// <summary>
        /// Cross-cutting audit observer. Logs every event that crosses the bus to the event log panel.
        /// Uses only the <see cref="BusEvent"/> base interface — no reflection, no type inspection.
        /// </summary>
        /// <param name="evt">The raw event object received from the bus.</param>
        private void OnAnyBusEvent(object evt)
        {
            if (evt is not BusEvent busEvent)
            {
                return;
            }

            LogEvent(busEvent.Category, busEvent.ToString() ?? "(unknown event)", busEvent.OccurredAt);
        }

        /// <summary>
        /// Handles double-tap on an event log row. Displays a dialog showing the full
        /// event message, allowing the user to read and copy details that were truncated
        /// in the grid view.
        /// </summary>
        /// <remarks>
        /// Uses async void — the only legitimate use of async void is for UI event handlers.
        /// </remarks>
        private async void OnEventLogRowDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (EventLogDataGrid.SelectedItem is not EventEntry entry)
            {
                return;
            }

            TextBlock textContent = new TextBlock
            {
                Text = $"Time:     {entry.OccurredAt.ToLocalTime():HH:mm:ss.fff}\n" +
                       $"Source:   {entry.Source}\n" +
                       $"Severity: {entry.Severity}\n\n" +
                       $"Message:\n{entry.Message}",
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
            };

            ScrollViewer scrollViewer = new ScrollViewer
            {
                Content = textContent,
                VerticalScrollMode = ScrollMode.Auto,
                HorizontalScrollMode = ScrollMode.Disabled,
                MaxHeight = 400,
                MaxWidth = 700
            };

            Button closeButton = new Button
            {
                Content = "Close",
                HorizontalAlignment = HorizontalAlignment.Center,
                MinWidth = 120,
                Margin = new Thickness(0, 16, 0, 0)
            };

            StackPanel panel = new StackPanel
            {
                Children = { scrollViewer, closeButton }
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = "Event Details",
                //Title = $"{entry.Source} — {entry.OccurredAt.ToLocalTime():HH:mm:ss}",
                Content = panel,
                XamlRoot = this.Content.XamlRoot
            };

            closeButton.Click += (s, args) =>
            {
                dialog.Hide();
            };

            await dialog.ShowAsync();
        }

        #endregion

        #region Event handlers for UI events

        private void ClearEventLog_Click(object sender, RoutedEventArgs e)
        {
            if (EventLogDataGrid.ItemsSource is ObservableCollection<EventEntry> eventLog)
            {
                eventLog.Clear();
            }
        }

        private void MutePlayer_Click(object sender, RoutedEventArgs e)
        {
            var selectedPlayer = PlayersListView.SelectedItem as PlayerDisplay;
            if (selectedPlayer != null)
            {
                // Fire a request to have the player muted/unmuted
                // TODO: Implement mute logic and update IsMuted property accordingly
            }
        }

        private void KickPlayer_Click(object sender, RoutedEventArgs e)
        {
            var selectedPlayer = PlayersListView.SelectedItem as PlayerDisplay;
            if (selectedPlayer != null)
            {
                // Kick logic here
                // TODO: Implement kick logic
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
                _eventBus.Publish(
                    EventMessageType.Network,
                    new NetworkEvents.Commands.StopListener());
            }
            else
            {
                // Publish event to start listener
                _eventBus.Publish(
                    EventMessageType.Network,
                    new NetworkEvents.Commands.StartListener());
            }

            ToggleListenerButton.IsEnabled = false; // Disable button until we get confirmation of state change            
        }

        private void SetActiveButton_Click(object sender, RoutedEventArgs e)
        {
            // Publish an event to request the server state change to ACTIVE
            _eventBus.Publish(
                EventMessageType.System,
                new SystemEvents.Commands.ServerStateChangeRequest(_serverState, ServerStateEnum.ACTIVE));
        }

        private void SetMaintenanceButton_Click(object sender, RoutedEventArgs e)
        {
            _eventBus.Publish(
                EventMessageType.System,
                new SystemEvents.Commands.ServerStateChangeRequest(_serverState, ServerStateEnum.MAINTENANCE));
        }

        private void SetShutdownButton_Click(object sender, RoutedEventArgs e)
        {
            _eventBus.Publish(
                EventMessageType.System,
                new SystemEvents.Commands.ServerStateChangeRequest(_serverState, ServerStateEnum.SHUTTING_DOWN));
        }


        private void OnWindowClosed(object sender, WindowEventArgs args)
        {
            _timer.Stop();
            _uptimeTimer.Stop();

            foreach (ISubscriptionToken subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }

        #endregion

        /// <summary>
        /// Subscribes to all event bus events relevant to the main window UI.
        /// Tokens are stored in <see cref="_subscriptions"/> and disposed on window close.
        /// </summary>
        private void SubscribeToEvents()
        {
            _subscriptions.Add(_eventBus.Subscribe<SystemEvents.Lifecycle.ServerStateChangedEvent>(EventMessageType.System, OnServerStateChanged));
            _subscriptions.Add(_eventBus.Subscribe<NetworkEvents.Lifecycle.ListenerStateChanged>(EventMessageType.Network, OnListenerStateChanged));
            _subscriptions.Add(_eventBus.Subscribe<WorldEvents.Notifications.PlayerEnteredWorldEvent>(EventMessageType.World, OnPlayerEnteredWorld));
            _subscriptions.Add(_eventBus.Subscribe<WorldEvents.Notifications.PlayerLeftWorldEvent>(EventMessageType.World, OnPlayerLeftWorld));
            _subscriptions.Add(_eventBus.Subscribe<MetricsEvents.Notifications.MetricSampleCollected>(EventMessageType.System, OnMetricCollected));

            // Cross-cutting audit observer — logs all bus events to the event log panel
            _subscriptions.Add(_eventBus.SubscribeAll(OnAnyBusEvent));
        }

        /// <summary>
        /// Marshals a formatted message onto the dispatcher thread and inserts it into the event log
        /// at the correct chronological position.
        /// </summary>
        /// <param name="source">The event bus category the event was published under.</param>
        /// <param name="message">The human-readable message to display.</param>
        /// <param name="occurredAt">The UTC time the event was originally published. Defaults to now if not provided.</param>
        private void LogEvent(EventMessageType source, string message, DateTime? occurredAt = null)
        {
            DateTime timestamp = occurredAt ?? DateTime.UtcNow;

            DispatcherQueue.TryEnqueue(() =>
            {
                EventEntry entry = ToEventEntry(timestamp, source, message);

                // Insert at the correct position to maintain descending chronological order
                int insertIndex = 0;
                while (insertIndex < _events.Count && _events[insertIndex].OccurredAt >= entry.OccurredAt)
                {
                    insertIndex++;
                }

                _events.Insert(insertIndex, entry);

                if (_events.Count > MaxEventLogEntries)
                {
                    _events.RemoveAt(_events.Count - 1);
                }
            });
        }

        /// <summary>
        /// Creates an <see cref="EventEntry"/> for display in the event log grid.
        /// </summary>
        /// <param name="time">The time the event occurred.</param>
        /// <param name="source">The event bus category the event was published under.</param>
        /// <param name="message">The human-readable message to display.</param>
        /// <returns>A populated <see cref="EventEntry"/>.</returns>
        public static EventEntry ToEventEntry(DateTime occurredAt, EventMessageType source, string message)
        {
            return new EventEntry
            {
                OccurredAt = occurredAt,
                Time = occurredAt.ToLocalTime().ToString("HH:mm:ss"),
                Source = source.ToString(),
                Message = message
            };
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
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
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