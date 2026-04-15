using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using Server.Core.Application;
using Server.Core.Infrastructure.Lifecycle;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Server.GUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        private SystemInitializer? _initializer;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // Instantiate the core system initializer (GUI must reference Server.Core).
                _initializer = new SystemInitializer();

                // Only pass the event bus to MainWindow (no direct lifecycle reference)
                _window = new MainWindow(_initializer.GetEventBus());

                // Ensure we stop server when window closes.
                _window.Closed += Window_Closed;

                _window.Activate();

                // Start the server off the UI thread in case startup is blocking.
                // Do this AFTER window is created and activated
                Task.Run(() =>
                {
                    try
                    {
                        _initializer.StartServer();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Server startup error: {ex}");
                        // Consider surfacing this to the UI via DispatcherQueue.TryEnqueue
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App launch error: {ex}");
                throw;
            }
        }

        private void Window_Closed(object? sender, WindowEventArgs e)
        {
            // Stop server gracefully when GUI window closes.
            try
            {
                _initializer?.StopServer();
            }
            catch (Exception)
            {
                // swallow or log; stopping should not crash the closing UI.
            }
            finally
            {
                _initializer = null;
            }
        }
    }
}
