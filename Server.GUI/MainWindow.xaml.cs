using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Server.Core.Infrastructure.Lifecycle;
using Shared.Domain.Player;
using Shared.EventBus;
using Shared.Identity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Graphics;

namespace Server.GUI
{
    public sealed partial class MainWindow : Window
    {
        private readonly IEventBus _eventBus;
        private ObservableCollection<PlayerDisplay> _activePlayers = new();
        private ObservableCollection<EventEntry> _events = new();
        private DispatcherTimer _timer = new();

        // Public property for XAML binding
        public ObservableCollection<EventEntry> Events => _events;


        public MainWindow(IEventBus eventBus)
        {
            try
            {
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("✓ InitializeComponent completed");

                _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
                System.Diagnostics.Debug.WriteLine("✓ EventBus validated");

                // Minimal setup - just set data sources and don't subscribe yet
                PlayersListView.ItemsSource = _activePlayers;
                EventLogDataGrid.ItemsSource = _events;
                System.Diagnostics.Debug.WriteLine("✓ ItemSources set");

                // Window sizing
                if (AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.PreferredMinimumWidth = 1080;
                    presenter.PreferredMinimumHeight = 940;
                    presenter.IsResizable = true;
                    presenter.IsMaximizable = true;
                    presenter.IsMinimizable = true;
                }
                this.AppWindow.Resize(new SizeInt32(1280, 1000));
                System.Diagnostics.Debug.WriteLine("✓ Window sizing applied");

                // Simple UI updates
                PlayerCountText.Text = $"PLAYERS CONNECTED: {_activePlayers.Count}";
                System.Diagnostics.Debug.WriteLine("✓ PlayerCountText updated");

                // Timer for server time
                _timer.Interval = TimeSpan.FromSeconds(1);
                _timer.Tick += (s, e) => ServerTimeText.Text = $"SERVER TIME: {DateTime.Now:HH:mm:ss}";
                _timer.Start();
                System.Diagnostics.Debug.WriteLine("✓ Clock timer started");

                // Uptime timer
                var startTime = DateTime.Now;
                var uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                uptimeTimer.Tick += (s, e) =>
                {
                    var uptime = DateTime.Now - startTime;
                    UptimeText.Text = uptime.ToString(@"hh\:mm\:ss");
                };
                uptimeTimer.Start();
                System.Diagnostics.Debug.WriteLine("✓ Uptime timer started");

                // Now try the event bus subscriptions
                System.Diagnostics.Debug.WriteLine("Attempting event bus subscriptions...");

                try
                {
                    _eventBus.Subscribe<ServerStateChangedEvent>(EventMessageType.System, OnServerStateChanged);
                    System.Diagnostics.Debug.WriteLine("✓ ServerStateChangedEvent subscription succeeded");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ ServerStateChangedEvent subscription failed: {ex.Message}");
                }

                try
                {
                    _eventBus.Subscribe<EventEnvelope>(EventMessageType.Log, OnLogEventReceived);
                    System.Diagnostics.Debug.WriteLine("✓ EventEnvelope subscription succeeded");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ EventEnvelope subscription failed: {ex.Message}");
                }

                try
                {
                    _eventBus.SubscribeAll(OnAnyEventReceived);
                    System.Diagnostics.Debug.WriteLine("✓ SubscribeAll succeeded");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ SubscribeAll failed: {ex.Message}");
                }

                System.Diagnostics.Debug.WriteLine("✓✓✓ MainWindow initialization COMPLETE ✓✓✓");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗✗✗ CRITICAL ERROR in MainWindow initialization: {ex.Message}");
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
                        StatusTextBlock.Text = "STATUS: ONLINE";
                        StatusTextBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                        break;
                    case ServerStateEnum.MAINTENANCE:
                        StatusTextBlock.Text = "STATUS: MAINTENANCE";
                        StatusTextBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 165, 0));
                        break;
                    case ServerStateEnum.SHUTTING_DOWN:
                        StatusTextBlock.Text = "STATUS: OFFLINE";
                        StatusTextBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 50, 50));
                        break;
                    case ServerStateEnum.LOADING:
                        StatusTextBlock.Text = "STATUS: LOADING";
                        StatusTextBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 170, 170, 170));
                        break;
                }
            });
        }

        private void OnConnectionStatusButtonClick(object sender, RoutedEventArgs e)
        {
            // Example: Toggle between online and maintenance for testing
            var newState = StatusTextBlock.Text.Contains("ONLINE") ? ServerStateEnum.MAINTENANCE : ServerStateEnum.ACTIVE;
            _eventBus.Publish(EventMessageType.System, new ServerStateChangeRequestedEvent(newState));
        }
    }

    public class PlayerDisplay
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Ping { get; set; } = "0ms";
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