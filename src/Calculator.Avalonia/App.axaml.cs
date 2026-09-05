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
                // Standard is the only mode with a ported view in Phase 2.
                ViewModel = new ApplicationViewModel();
                ViewModel.Initialize(ViewMode.Standard);

                desktop.MainWindow = new MainWindow
                {
                    DataContext = ViewModel,
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
