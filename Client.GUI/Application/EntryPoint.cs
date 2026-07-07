// @file      EntryPoint.cs
// @namespace Client.GUI.Application
// @brief     Native entry point for the MUDDY client GUI. Configures per-monitor
//            DPI awareness, bootstraps the Windows App SDK runtime, and starts
//            the WinUI 3 message loop.
// @details   Bootstrap.Initialize must be called before any Windows App SDK API.
//            Application.Start blocks until the application shuts down.

using XamlApplication = Microsoft.UI.Xaml.Application;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Client.GUI.Application
{
    internal static partial class EntryPoint
    {
        [LibraryImport("user32.dll")]
        private static partial int SetProcessDpiAwarenessContext(IntPtr value); // partial, not extern

        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);

        static void Main()
        {
            // Set DPI awareness
            _ = SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

            Bootstrap.Initialize(0x00010008);

            XamlApplication.Start(p =>
            {
                _ = new App();
            });
        }
    }
}