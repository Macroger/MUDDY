// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Client.GUI
{
    internal static class EntryPoint
    {

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [STAThread]
        [SuppressMessage("Interoperability", "CA1416", Justification = "TFM guarantees Windows 10.0.19041+")]
        static void Main(string[] args)
        {
            // Set DPI awareness
            SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

            Bootstrap.Initialize(0x00010008);

            Application.Start(callback =>
            {
                _ = new App();
            });
        }
    }
}