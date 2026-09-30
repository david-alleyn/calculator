// Entry point for the Avalonia/Linux calculator (Phase 2).

using System;
using Avalonia;

namespace CalculatorApp.Avalonia
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        }
    }
}
