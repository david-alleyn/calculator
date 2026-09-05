// Headless host for the Avalonia view tests: runs the real App (with its
// Fluent theme and calculator styles) but without a desktop lifetime.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;

namespace Calculator.Avalonia.Tests
{
    public class AvaloniaTestApp : CalculatorApp.Avalonia.App
    {
        public override void OnFrameworkInitializationCompleted()
        {
            // No MainWindow here; tests create and dispose windows themselves.
            base.OnFrameworkInitializationCompleted();
        }
    }

    internal static class AvaloniaTestHost
    {
        public static AppBuilder BuildHeadlessApp()
        {
            return AppBuilder
                .Configure<AvaloniaTestApp>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions
                {
                    UseHeadlessDrawing = true,
                });
        }
    }
}
