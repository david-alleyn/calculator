using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

using CalculatorApp.Avalonia.Common;
using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;
using CalculatorApp.ViewModel.Snapshot;

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

            // QA/user knob: CALCULATOR_UI_CULTURE=fr-FR pins the UI culture
            // (useful when the host has no matching OS locale).
            string uiCulture = Environment.GetEnvironmentVariable("CALCULATOR_UI_CULTURE");
            if (!string.IsNullOrEmpty(uiCulture))
            {
                try
                {
                    var culture = new System.Globalization.CultureInfo(uiCulture);
                    System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
                    System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
                    System.Globalization.CultureInfo.CurrentCulture = culture;
                    System.Globalization.CultureInfo.CurrentUICulture = culture;
                }
                catch (System.Globalization.CultureNotFoundException)
                {
                }
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // QA/automation knob: CALCULATOR_MODE=Standard|Scientific|... 
                ViewMode startMode = ViewMode.Standard;
                bool modeForced = false;
                string modeName = Environment.GetEnvironmentVariable("CALCULATOR_MODE");
                if (!string.IsNullOrEmpty(modeName)
                    && Enum.TryParse(modeName, ignoreCase: true, out ViewMode parsedMode))
                {
                    startMode = parsedMode;
                    modeForced = true;
                }

                ViewModel = new ApplicationViewModel();
                ViewModel.Initialize(startMode);

                // Desktop lifecycle: restore the previous session unless a QA
                // mode override was requested.
                if (!modeForced)
                {
                    ApplicationSnapshot saved = SnapshotStore.TryLoad();
                    if (saved != null && NavCategoryStates.IsViewModeEnabled((ViewMode)saved.Mode))
                    {
                        try
                        {
                            ViewModel.RestoreFromSnapshot(saved);
                        }
                        catch
                        {
                            // Corrupt snapshots fall back to the initialized mode.
                        }
                    }
                }

                desktop.MainWindow = new MainWindow
                {
                    DataContext = ViewModel,
                };

                // Route the ViewModel clipboard facade to the system clipboard.
                LinuxClipboard.SetProvider(
                    new AvaloniaClipboardProvider(() => desktop.MainWindow?.Clipboard));

                desktop.ShutdownRequested += (_, _) => SnapshotStore.Save(ViewModel.Snapshot);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
