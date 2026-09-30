// View-layer tests for the mode-switching keyboard accelerators.

using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CalculatorApp.Avalonia;
using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Calculator.Avalonia.Tests
{
    [TestClass]
    public class ModeShortcutTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            AvaloniaTestHost.EnsureInitialized();
        }

        private static (MainWindow window, ApplicationViewModel viewModel) OpenCalculator()
        {
            var viewModel = new ApplicationViewModel();
            viewModel.Initialize(ViewMode.Standard);

            MainWindow window = null;
            Dispatcher.UIThread.Post(() =>
            {
                window = new MainWindow { DataContext = viewModel };
                window.Show();
            });
            Dispatcher.UIThread.RunJobs();

            Assert.IsNotNull(window);
            return (window, viewModel);
        }

        [TestMethod]
        public void ModeShortcuts_SwitchBetweenPortedModes()
        {
            var (window, viewModel) = OpenCalculator();

            Assert.IsTrue(window.TryHandleModeShortcut(Key.D2, KeyModifiers.Control));
            Assert.AreEqual(ViewMode.Scientific, viewModel.Mode);

            Assert.IsTrue(window.TryHandleModeShortcut(Key.D4, KeyModifiers.Alt));
            Assert.AreEqual(ViewMode.Programmer, viewModel.Mode);

            Assert.IsTrue(window.TryHandleModeShortcut(Key.D5, KeyModifiers.Control));
            Assert.AreEqual(ViewMode.Date, viewModel.Mode);

            Assert.IsTrue(window.TryHandleModeShortcut(Key.D1, KeyModifiers.Control));
            Assert.AreEqual(ViewMode.Standard, viewModel.Mode);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ModeShortcut_RequiresModifier()
        {
            var (window, _) = OpenCalculator();

            Assert.IsFalse(window.TryHandleModeShortcut(Key.D2, KeyModifiers.None));

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ModeShortcut_UnportedMode_IsIgnored()
        {
            var (window, viewModel) = OpenCalculator();

            // Graphing (3) is not ported and therefore disabled in the nav.
            Assert.IsFalse(window.TryHandleModeShortcut(Key.D3, KeyModifiers.Control));
            Assert.AreEqual(ViewMode.Standard, viewModel.Mode);

            Dispatcher.UIThread.Post(window.Close);
        }
    }
}
