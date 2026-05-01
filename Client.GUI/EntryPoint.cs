using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Client.GUI
{
    internal static class EntryPoint
    {
        [STAThread]
        [SuppressMessage("Interoperability", "CA1416", Justification = "TFM guarantees Windows 10.0.19041+")]
        static void Main(string[] args)
        {
            Bootstrap.Initialize(0x00010008);

            Application.Start(callback =>
            {
                _ = new App();
            });
        }
    }
}