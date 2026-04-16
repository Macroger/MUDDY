using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Server.Core.Infrastructure.Lifecycle;
using Shared.Domain.Player;
using Shared.EventBus;
using Shared.EventBus.SubscriptionToken;
using Shared.Identity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Graphics;
using static Shared.EventBus.DomainEvents.NetworkEvents;

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

        private string _listenerStateText = "Stopped";
        private Brush _listenerStateBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);

        public event PropertyChangedEventHandler PropertyChanged;
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
                    // Subscribe to ServerStateChangeEvent
                    _eventSubscriptions.Add(_eventBus.Subscribe<ServerStateChangedEvent>(EventMessageType.System, OnServerStateChanged));

                    // Subscribe to all events for logging purposes (could be filtered or categorized in a real app)
                    _eventSubscriptions.Add(_eventBus.SubscribeAll(OnAnyEventReceived));

                    // Subscribe to log events specifically for structured logging
                    _eventSubscriptions.Add(_eventBus.Subscribe<EventEnvelope>(EventMessageType.Log, OnLogEventReceived));

                    // Subscribe to ListenerStateChangedEvent 
                    _eventSubscriptions.Add(_eventBus.Subscribe<ListenerStateChangedEvent>(EventMessageType.Network, OnListenerStateChanged));
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

        private void OnLogEventReceived(EventEnvelope envelope)
        {
            // Assume the payload is an EventReason (see EventBusHelper)
            if (envelope.Payload is EventReason reason)
            {
                // UI updates must be on the UI thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    _events.Insert(0, ToEventEntry(DateTime.Now, "Server", reason.Message));

                    // Optionally trim log size
                    if (_events.Count > 100) _events.RemoveAt(_events.Count - 1);
                });
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
                Location = state.CurrentLocation.ToString()
            };
        }

        private void OnAnyEventReceived(object evt)
        {
            string type = evt.GetType().Name;
            string message = string.Empty;

            switch (evt)
            {
                case EventReason reason:
                    string detailsString = "";
                    if (reason.Data is null)
                    {
                        detailsString = "(none)";
                    }
                    else if (reason.Data is string s)
                    {
                        detailsString = s;
                    }
                    else if (reason.Data is Exception ex)
                    {
                        detailsString = $"{ex.Message}\n{ex.StackTrace}";
                    }
                    else if (reason.Data is System.Collections.IDictionary dict)
                    {
                        var items = new List<string>();
                        foreach (var key in dict.Keys)
                            items.Add($"{key}: {dict[key]}");
                        detailsString = string.Join(", ", items);
                    }
                    else if (reason.Data is System.Collections.IEnumerable enumerable && !(reason.Data is string))
                    {
                        var items = new List<string>();
                        foreach (var item in enumerable)
                            items.Add(item?.ToString());
                        detailsString = "[" + string.Join(", ", items) + "]";
                    }
                    else
                    {
                        detailsString = reason.Data.ToString();
                    }
                    message = $"Reason: {reason.Message} | Details: {detailsString}";
                    break;
                case ServerStateChangedEvent stateChanged:
                    message = $"State changed from {stateChanged.PreviousState} to {stateChanged.NewState}";
                    break;
                case ServerStateChangeRequestedEvent stateRequest:
                    message = $"State change requested: {stateRequest.RequestedState}";
                    break;
                case EventEnvelope envelope:
                    message = $"Envelope: {envelope.Payload?.GetType().Name ?? "null"} | {envelope.Payload}";
                    break;
                default:
                    message = evt.ToString() ?? "(null event)";
                    break;
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                _events.Insert(0, ToEventEntry(DateTime.Now, type, message));
                if (_events.Count > 100) _events.RemoveAt(_events.Count - 1);
            });
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

        private void OnListenerStateChanged(ListenerStateChangedEvent evnt)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if(evnt.IsListenerStarted == true)
                {
                    _listenerStateText = "ONLINE";
                    ListenerStateText = _listenerStateText;
                    _listenerStateBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
                }
                else
                {
                    _listenerStateText = "OFFLINE";
                    ListenerStateText = _listenerStateText;
                    _listenerStateBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
                }
                
            });
        }

        private void OnConnectionStatusButtonClick(object sender, RoutedEventArgs e)
        {
            var newState = StatusTextBlock.Text.Contains("ONLINE") ? ServerStateEnum.MAINTENANCE : ServerStateEnum.ACTIVE;
            _eventBus.Publish(EventMessageType.System, new ServerStateChangeRequestedEvent(newState));
        }

        private void MutePlayer_Click(object sender, RoutedEventArgs e)
        {
            var selectedPlayer = PlayersListView.SelectedItem as PlayerDisplay;
            if (selectedPlayer != null)
            {
                // Mute logic here
                // Example: _eventBus.Publish(...);
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




}