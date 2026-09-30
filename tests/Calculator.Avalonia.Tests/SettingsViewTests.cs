// View-layer smoke tests for the Settings page: opening from the navigation
// pane, theme selection, and back navigation.

using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CalculatorApp.Avalonia;
using CalculatorApp.Avalonia.Views;
using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Calculator.Avalonia.Tests
{
    [TestClass]
    public class SettingsViewTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            AvaloniaTestHost.EnsureInitialized();
        }

        private static (Window window, ApplicationViewModel viewModel) OpenCalculator()
        {
            var viewModel = new ApplicationViewModel();
            viewModel.Initialize(ViewMode.Standard);

            Window window = null;
            Dispatcher.UIThread.Post(() =>
            {
                window = new MainWindow { DataContext = viewModel };
                window.Show();
            });
            Dispatcher.UIThread.RunJobs();

            Assert.IsNotNull(window);
            return (window, viewModel);
        }

        private static T FindIn<T>(Control root, string name) where T : Control
        {
            return root
                .GetVisualDescendants()
                .OfType<T>()
                .FirstOrDefault(control => control.Name == name);
        }

        [TestMethod]
        public void Settings_OpensFromNavPane_AndBackRestoresMode()
        {
            var (window, _) = OpenCalculator();

            var settings = FindIn<SettingsView>(window, "Settings");
            var calculator = FindIn<CalculatorView>(window, "Calculator");
            var settingsButton = FindIn<Button>(window, "SettingsButton");
            Assert.IsNotNull(settings);
            Assert.IsNotNull(calculator);
            Assert.IsNotNull(settingsButton);

            Assert.IsFalse(settings.IsVisible);

            settingsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.IsTrue(settings.IsVisible, "Settings should open from the nav pane.");
            Assert.IsFalse(calculator.IsVisible, "The mode view should be hidden while Settings is open.");

            var back = FindIn<Button>(settings, "BackButton");
            Assert.IsNotNull(back);
            back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.IsFalse(settings.IsVisible, "Back should close Settings.");
            Assert.IsTrue(calculator.IsVisible, "Back should restore the mode view.");

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void Settings_ThemeSelection_UpdatesThemeVariant()
        {
            var (window, _) = OpenCalculator();
            var settings = FindIn<SettingsView>(window, "Settings");
            Assert.IsNotNull(settings);

            // The settings content attaches on first layout; open it first.
            var settingsButton = FindIn<Button>(window, "SettingsButton");
            Assert.IsNotNull(settingsButton);
            settingsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            var dark = FindIn<RadioButton>(settings, "DarkThemeRadioButton");
            var light = FindIn<RadioButton>(settings, "LightThemeRadioButton");
            Assert.IsNotNull(dark);
            Assert.IsNotNull(light);

            dark.IsChecked = true;
            dark.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.AreEqual(ThemeVariant.Dark, Application.Current.RequestedThemeVariant);

            light.IsChecked = true;
            light.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.AreEqual(ThemeVariant.Light, Application.Current.RequestedThemeVariant);

            // Restore the default so other tests are unaffected.
            Application.Current.RequestedThemeVariant = ThemeVariant.Default;

            Dispatcher.UIThread.Post(window.Close);
        }
    }
}
