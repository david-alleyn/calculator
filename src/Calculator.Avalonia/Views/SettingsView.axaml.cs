// Settings page code-behind: theme selection and back navigation.

using System;
using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace CalculatorApp.Avalonia.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            SyncThemeSelection();
        }

        // Raised when the user taps the back button; the shell restores the
        // previous mode's view.
        public event EventHandler BackRequested;

        private void SyncThemeSelection()
        {
            ThemeVariant variant = Application.Current?.RequestedThemeVariant;
            LightThemeRadioButton.IsChecked = variant == ThemeVariant.Light;
            DarkThemeRadioButton.IsChecked = variant == ThemeVariant.Dark;
            SystemThemeRadioButton.IsChecked = variant == ThemeVariant.Default;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current == null)
            {
                return;
            }

            if (sender == LightThemeRadioButton)
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            }
            else if (sender == DarkThemeRadioButton)
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
            }
            else
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Default;
            }
        }

        private void Feedback_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/david-alleyn/calculator/issues",
                    UseShellExecute = true,
                });
            }
            catch
            {
                // Feedback is best-effort; a missing browser must not crash the app.
            }
        }
    }
}
