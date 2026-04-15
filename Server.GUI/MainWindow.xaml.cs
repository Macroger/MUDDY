using Microsoft.UI.Xaml;
using Shared.EventBus;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using Windows.Graphics;

namespace Server.GUI
{
    public sealed partial class MainWindow : Window
    {
        private readonly IEventBus _eventBus;
        private ObservableCollection<PlayerEntry> _players = new();
        private ObservableCollection<EventEntry> _events = new();
        private DispatcherTimer _timer = new();

        public MainWindow()
        {
            InitializeComponent();

            // Set window size
            this.AppWindow.Resize(new SizeInt32(1200, 750));

            // Placeholder players
            _players.Add(new PlayerEntry { Name = "Aria", Location = "Goblin Cave", Ping = "52 ms" });
            _players.Add(new PlayerEntry { Name = "Thorn", Location = "Dark Forest", Ping = "38 ms" });
            _players.Add(new PlayerEntry { Name = "Eldrin", Location = "Town Square", Ping = "47 ms" });
            PlayersListView.ItemsSource = _players;
            PlayerCountText.Text = $"PLAYERS: {_players.Count}";

            // Placeholder events
            EventLogListView.ItemsSource = _events;

            // Clock timer
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => ServerTimeText.Text = $"SERVER TIME: {DateTime.Now:HH:mm:ss}";
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

            // --- EventBus wiring example: subscribe to Log events ---
            _eventBus = new BasicEventBus();
            _eventBus.Subscribe<EventEnvelope>(EventMessageType.Log, envelope =>
            {
                // Assume the payload is an EventReason (see EventBusHelper)
                if (envelope.Payload is EventReason reason)
                {
                    // UI updates must be on the UI thread
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        _events.Insert(0, new EventEntry
                        {
                            Time = DateTime.Now.ToString("HH:mm:ss"),
                            Source = "Server",
                            Message = reason.Message,
                            Severity = "Info"
                        });

                        // Optionally trim log size
                        if (_events.Count > 100) _events.RemoveAt(_events.Count - 1);
                    });
                }
            });
            // --- End EventBus wiring example ---
        }
    }

    public class PlayerEntry
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string Ping { get; set; }
    }

    public class EventEntry
    {
        public string Time { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }
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