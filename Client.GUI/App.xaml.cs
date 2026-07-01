// =============================================================================
using Client.Core.Application;
/// @file       App.xaml.cs
/// @namespace  Client.GUI
/// @brief      WinUI 3 application entry point for MUDDY client.
///             Initializes core systems and manages application lifecycle.
// =============================================================================

using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;

namespace Client.GUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window = null;
        private ClientSystemInitializer? _clientCore = null;

        /// <summary>
        /// Current application settings loaded at startup.
        /// </summary>
        public static AppSettings Settings { get; private set; } = new AppSettings();


        public enum AppTheme
        {
            Default,
            Psychedelic,
            Dark,
            Light
        }


        private static readonly Dictionary<AppTheme, string> ThemeMap = new()
        {
            { AppTheme.Default,      "ms-appx:///Themes/DefaultTheme.xaml" },
            { AppTheme.Psychedelic,  "ms-appx:///Themes/PsychedelicTheme.xaml" },
            { AppTheme.Dark,         "ms-appx:///Themes/DarkTheme.xaml" },
            { AppTheme.Light,        "ms-appx:///Themes/LightTheme.xaml" }
        };

        private AppTheme _currentTheme = AppTheme.Default;



        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            // Add global exception handler to catch and log any unhandled exceptions
            UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Mark as handled to prevent app crash
            e.Handled = true;

            string errorMessage = $"UNHANDLED EXCEPTION at {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                 $"Message: {e.Message}\n" +
                                 $"Exception: {e.Exception?.ToString() ?? "Unknown"}\n" +
                                 $"Stack Trace: {e.Exception?.StackTrace ?? "No stack trace"}\n\n";

            // Try to write to error log in safe location
            try
            {
                string errorLogPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MUDDY", "Logs", "client_errors.log");

                Directory.CreateDirectory(Path.GetDirectoryName(errorLogPath)!);
                File.AppendAllText(errorLogPath, errorMessage);
            }
            catch
            {
                // If that fails, try temp directory
                try
                {
                    string tempErrorLog = Path.Combine(Path.GetTempPath(), "MUDDY_client_error.log");
                    File.AppendAllText(tempErrorLog, errorMessage);
                }
                catch
                {
                    // Last resort - ignore if we can't log
                }
            }

            // Always write to console/debug output
            Console.WriteLine(errorMessage);
            System.Diagnostics.Debug.WriteLine(errorMessage);
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // Load settings first
                Settings = AppSettingsManager.Load();

                // Initialize client core systems (event bus, logger, network supervisor, message pipeline)
                _clientCore = new ClientSystemInitializer();

                // Create main window and pass event bus
                _window = new MainWindow(_clientCore.EventBus);

                ThemeManager.Initialize(_window);
                ThemeManager.Apply(Settings.SelectedTheme);

                // Cleanup when window closes
                _window.Closed += Window_Closed;

                _window.Activate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App launch error: {ex}");
                throw;
            }
        }

        private void LoadSavedTheme()
        {
            ApplyTheme(Settings.SelectedTheme);
        }

        public void ApplyTheme(AppTheme theme)
        {
            System.Diagnostics.Debug.WriteLine($"[Theme] APPLY CALLED: {theme}");

            var dictionaries = Application.Current.Resources.MergedDictionaries;

            var newTheme = new ResourceDictionary
            {
                Source = new Uri(ThemeMap[theme])
            };

            // Find current theme using EXACT match (no guessing)
            for (int i = 0; i < dictionaries.Count; i++)
            {
                var src = dictionaries[i].Source?.OriginalString;

                if (src == ThemeMap[_currentTheme])
                {
                    System.Diagnostics.Debug.WriteLine($"[Theme] Replacing {src}");

                    dictionaries[i] = newTheme;

                    _currentTheme = theme;
                    return;
                }
            }

            // fallback (only happens once at startup or if something is off)
            System.Diagnostics.Debug.WriteLine("[Theme] No existing theme found, adding new one");
            dictionaries.Add(newTheme);

            _currentTheme = theme;
        }

        private void Window_Closed(object? sender, WindowEventArgs e)
        {
            // Dispose client systems gracefully when GUI window closes
            try
            {
                _clientCore?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during client shutdown: {ex.Message}");
            }
            finally
            {
                _clientCore = null;
            }
        }
    }
}