using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Windows.UI;
using System;
using System.Collections.Generic;

namespace MuddyClient.Pages
{
    public sealed partial class MainGamePage : Page
    {
        private List<string> commandHistory = new List<string>();
        private int historyIndex = -1;

        public MainGamePage()
        {
            this.InitializeComponent();
            LoadPlaceholderData();
        }

        private void LoadPlaceholderData()
        {
            txtHP.Text = "HP:   100";
            txtMana.Text = "Mana: 50";
            txtXP.Text = "XP:   2200";

            AddInventoryItem("🗡️ Sword");
            AddInventoryItem("🧪 Health Potion");
            AddInventoryItem("🔑 Key");
            AddInventoryItem("🔦 Torch");
            AddInventoryItem("Empty Slot");

            AppendGameText("You are in a dimly lit cavern.", "#FFFFFF");
            AppendGameText("A foul goblin snarls and prepares to attack!", "#FFFFFF");
            AppendGameText("The goblin swings at you and misses.", "#FFFFFF");
            AppendGameText("A player named \"Aria\" enters the room.", "#FFFFFF");
            AppendGameText("Aria says: \"Let's take it down!\"", "#F4A300");
            AppendGameText("There are exits to the North and East.", "#AAAAAA");
        }

        // ── Game Text ────────────────────────────────────────────────
        public void AppendGameText(string message, string hexColor = "#FFFFFF")
        {
            var para = new Paragraph();
            var run = new Run
            {
                Text = message,
                Foreground = new SolidColorBrush(ParseHex(hexColor))
            };
            para.Inlines.Add(run);
            GameTextBox.Blocks.Add(para);
            GameScrollViewer.ChangeView(null, double.MaxValue, null);
        }

        private Color ParseHex(string hex)
        {
            hex = hex.TrimStart('#');
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Color.FromArgb(255, r, g, b);
        }

        // ── Inventory ────────────────────────────────────────────────
        private void AddInventoryItem(string itemName)
        {
            var tb = new TextBlock
            {
                Text = itemName,
                Foreground = new SolidColorBrush(itemName == "Empty Slot"
                    ? Color.FromArgb(255, 120, 120, 120)
                    : Color.FromArgb(255, 220, 220, 220)),
                FontSize = 13,
                Padding = new Thickness(4, 4, 4, 4)
            };
            var sep = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = tb
            };
            InventoryList.Children.Add(sep);
        }

        // ── Command Input ────────────────────────────────────────────
        private void SendButton_Click(object sender, RoutedEventArgs e) => SendCommand();

        private void CommandInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SendCommand();
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Up)
            {
                NavigateHistory(-1);
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Down)
            {
                NavigateHistory(1);
                e.Handled = true;
            }
        }

        private void SendCommand()
        {
            string command = CommandInput.Text.Trim();
            if (string.IsNullOrEmpty(command)) return;

            AppendGameText($"> {command}", "#7EC8E3");
            commandHistory.Insert(0, command);
            historyIndex = -1;

            // TODO: replace with real networking call
            AppendGameText("Command sent to server...", "#888888");

            CommandInput.Text = "";
            CommandInput.Focus(FocusState.Programmatic);
        }

        private void NavigateHistory(int direction)
        {
            if (commandHistory.Count == 0) return;
            historyIndex = Math.Clamp(historyIndex + direction, 0, commandHistory.Count - 1);
            CommandInput.Text = commandHistory[historyIndex];
            CommandInput.SelectionStart = CommandInput.Text.Length;
        }

        // ── Spell Buttons ────────────────────────────────────────────
        private void SpellButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                CommandInput.Text = $"cast {btn.Content}";
                CommandInput.Focus(FocusState.Programmatic);
                CommandInput.SelectionStart = CommandInput.Text.Length;
            }
        }

        // ── System Options ───────────────────────────────────────────
        private void Settings_Click(object sender, RoutedEventArgs e)
            => AppendGameText("[System] Settings not yet implemented.", "#888888");

        private void Logs_Click(object sender, RoutedEventArgs e)
            => AppendGameText("[System] Logs not yet implemented.", "#888888");

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            AppendGameText("=== HELP ===", "#F4A300");
            AppendGameText("Commands: move <dir>, attack <target>, cast <spell>, pick <item>, drop <item>, look, inventory", "#AAAAAA");
        }
    }
}