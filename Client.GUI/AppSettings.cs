// =============================================================================
/// @file       AppSettings.cs
/// @namespace  Client.GUI
/// @brief      Application settings model for persistent user preferences.
///             Stored as JSON in %LocalAppData%\MUDDY\settings.json
// =============================================================================

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Client.GUI.App;

namespace Client.GUI
{
    /// <summary>
    /// Represents all user-configurable settings for the MUDDY client.
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>
        /// Selected theme name (e.g., "Default", "Psychedelic", "Dark Mode", "Light Mode").
        /// </summary>
        [JsonPropertyName("theme")]
        public AppTheme SelectedTheme { get; set; } = AppTheme.Default;

        /// <summary>
        /// Whether notifications are enabled.
        /// </summary>
        [JsonPropertyName("notificationsEnabled")]
        public bool NotificationsEnabled { get; set; } = true;

        /// <summary>
        /// Whether to auto-connect on startup.
        /// </summary>
        [JsonPropertyName("autoConnect")]
        public bool AutoConnectOnStartup { get; set; } = false;

        /// <summary>
        /// Whether to remember the last connected server.
        /// </summary>
        [JsonPropertyName("rememberServer")]
        public bool RememberLastServer { get; set; } = true;

        /// <summary>
        /// Game output font size.
        /// </summary>
        [JsonPropertyName("fontSize")]
        public double GameOutputFontSize { get; set; } = 14.0;

        /// <summary>
        /// Last connected server address (if RememberLastServer is true).
        /// </summary>
        [JsonPropertyName("lastServerAddress")]
        public string LastServerAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// Last connected server port (if RememberLastServer is true).
        /// </summary>
        [JsonPropertyName("lastServerPort")]
        public int LastServerPort { get; set; } = 30333;
    }

    /// <summary>
    /// Manages loading and saving application settings to disk.
    /// </summary>
    public static class AppSettingsManager
    {
        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MUDDY");

        private static readonly string SettingsFilePath = Path.Combine(SettingsFolder, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new System.Text.Json.Serialization.JsonStringEnumConverter()
            }

        };

        /// <summary>
        /// Loads settings from disk. Returns default settings if file doesn't exist or fails to load.
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("Settings file not found. Using defaults.");
                    return new AppSettings();
                }

                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

                System.Diagnostics.Debug.WriteLine($"Settings loaded from: {SettingsFilePath}");
                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}. Using defaults.");
                return new AppSettings();
            }
        }

        /// <summary>
        /// Saves settings to disk.
        /// </summary>
        public static void Save(AppSettings settings)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(SettingsFolder);

                // Serialize and write
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);

                System.Diagnostics.Debug.WriteLine($"Settings saved to: {SettingsFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
                throw; // Re-throw so caller can handle the error
            }
        }

        /// <summary>
        /// Returns the full path to the settings file (useful for debugging).
        /// </summary>
        public static string GetSettingsFilePath() => SettingsFilePath;
    }
}