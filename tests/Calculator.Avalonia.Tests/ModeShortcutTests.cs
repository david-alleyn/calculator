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

            Assert.IsTrue(window.TryHandleShortcut(Key.D2, KeyModifiers.Control));
            Assert.AreEqual(ViewMode.Scientific, viewModel.Mode);

            Assert.IsTrue(window.TryHandleShortcut(Key.D4, KeyModifiers.Alt));
            Assert.AreEqual(ViewMode.Programmer, viewModel.Mode);

            Assert.IsTrue(window.TryHandleShortcut(Key.D5, KeyModifiers.Control));
            Assert.AreEqual(ViewMode.Date, viewModel.Mode);

            Assert.IsTrue(window.TryHandleShortcut(Key.D1, KeyModifiers.Control));
            Assert.AreEqual(ViewMode.Standard, viewModel.Mode);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ModeShortcut_RequiresModifier()
        {
            var (window, _) = OpenCalculator();

            Assert.IsFalse(window.TryHandleShortcut(Key.D2, KeyModifiers.None));

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ModeShortcut_UnportedMode_IsIgnored()
        {
            var (window, viewModel) = OpenCalculator();

            // Graphing (3) is not ported and therefore disabled in the nav.
            Assert.IsFalse(window.TryHandleShortcut(Key.D3, KeyModifiers.Control));
            Assert.AreEqual(ViewMode.Standard, viewModel.Mode);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void CommandShortcuts_ManageMemory()
        {
            var (window, viewModel) = OpenCalculator();
            var calculator = viewModel.CalculatorViewModel;

            calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.Five);

            Assert.IsTrue(window.TryHandleShortcut(Key.M, KeyModifiers.Control));
            Assert.AreEqual(1, calculator.MemorizedNumbers.Count);
            Assert.AreEqual("5", calculator.MemorizedNumbers[0].Value);

            Assert.IsTrue(window.TryHandleShortcut(Key.L, KeyModifiers.Control));
            Assert.AreEqual(0, calculator.MemorizedNumbers.Count);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void CommandShortcut_History_IsHandled()
        {
            var (window, _) = OpenCalculator();

            Assert.IsTrue(window.TryHandleShortcut(Key.H, KeyModifiers.Control));

            Dispatcher.UIThread.Post(window.Close);
        }
    }
}
