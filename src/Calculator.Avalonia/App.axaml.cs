using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

namespace CalculatorApp.Avalonia
{
    public class App : Application
    {
        public ApplicationViewModel ViewModel { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // QA/automation knob: CALCULATOR_THEME=light|dark forces a variant.
            switch (Environment.GetEnvironmentVariable("CALCULATOR_THEME"))
            {
                case "light":
                    RequestedThemeVariant = ThemeVariant.Light;
                    break;
                case "dark":
                    RequestedThemeVariant = ThemeVariant.Dark;
                    break;
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // QA/automation knob: CALCULATOR_MODE=Standard|Scientific|... 
                ViewMode startMode = ViewMode.Standard;
                string modeName = Environment.GetEnvironmentVariable("CALCULATOR_MODE");
                if (!string.IsNullOrEmpty(modeName)
                    && Enum.TryParse(modeName, ignoreCase: true, out ViewMode parsedMode))
                {
                    startMode = parsedMode;
                }

                ViewModel = new ApplicationViewModel();
                ViewModel.Initialize(startMode);

                desktop.MainWindow = new MainWindow
                {
                    DataContext = ViewModel,
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
