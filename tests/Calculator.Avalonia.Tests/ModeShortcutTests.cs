// View-layer tests for the mode-switching keyboard accelerators.

using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CalculatorApp.Avalonia;
using CalculatorApp.Avalonia.Controls;
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
            return OpenCalculator(ViewMode.Standard);
        }

        private static (MainWindow window, ApplicationViewModel viewModel) OpenCalculator(ViewMode mode)
        {
            var viewModel = new ApplicationViewModel();
            viewModel.Initialize(mode);

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
        public void EnterKey_AlwaysMeansEquals_EvenWithAKeypadButtonFocused()
        {
            var (window, viewModel) = OpenCalculator();
            var calculator = viewModel.CalculatorViewModel;

            calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.Seven);
            calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.Add);
            calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.Eight);

            // Clicking a keypad button leaves it focused; Return must still
            // compute (7 + 8), not re-press the focused 8.
            var button = window.GetVisualDescendants().OfType<CalculatorButton>()
                .First(candidate => candidate.Name == "Num8Button");
            Dispatcher.UIThread.Post(() => button.Focus());
            Dispatcher.UIThread.RunJobs();

            button.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Enter,
                KeyModifiers = KeyModifiers.None,
            });
            Dispatcher.UIThread.RunJobs();

            Assert.AreEqual("15", calculator.DisplayValue);

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

        [TestMethod]
        public void ScientificChord_Sinh_Computes()
        {
            var (window, viewModel) = OpenCalculator(ViewMode.Scientific);
            var calculator = viewModel.CalculatorViewModel;

            calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.One);

            Assert.IsTrue(window.TryHandleShortcut(Key.S, KeyModifiers.Control));
            Assert.IsTrue(calculator.DisplayValue.StartsWith("1.175"),
                $"sinh(1) should be about 1.175; got '{calculator.DisplayValue}'.");

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ScientificChord_IsInactiveInStandardMode()
        {
            var (window, _) = OpenCalculator(ViewMode.Standard);

            Assert.IsFalse(window.TryHandleShortcut(Key.S, KeyModifiers.Control));

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ClipboardShortcut_CopiesAndPastesDisplayValue()
        {
            var (window, viewModel) = OpenCalculator(ViewMode.Standard);
            var calculator = viewModel.CalculatorViewModel;

            calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.Four);
            calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.Two);

            Assert.IsTrue(window.TryHandleShortcut(Key.C, KeyModifiers.Control));

            calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.Clear);
            Assert.AreEqual("0", calculator.DisplayValue);

            Assert.IsTrue(window.TryHandleShortcut(Key.V, KeyModifiers.Control));
            Dispatcher.UIThread.RunJobs();

            Assert.AreEqual("42", calculator.DisplayValue);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void AccessKeyHints_ToggleOnMainView()
        {
            var (window, _) = OpenCalculator();
            var main = window.GetVisualDescendants().OfType<CalculatorApp.Avalonia.Views.MainView>().First();
            var navPane = window.GetVisualDescendants().OfType<Border>()
                .First(control => control.Name == "NavPane");

            Assert.IsFalse(navPane.Classes.Contains("AccessKeyHintsVisible"));

            main.SetAccessKeyHintsVisible(true);
            Assert.IsTrue(navPane.Classes.Contains("AccessKeyHintsVisible"));

            main.SetAccessKeyHintsVisible(false);
            Assert.IsFalse(navPane.Classes.Contains("AccessKeyHintsVisible"));

            Dispatcher.UIThread.Post(window.Close);
        }
    }
}

namespace Calculator.Avalonia.Tests
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    public class RtlLayoutTests
    {
        [Microsoft.VisualStudio.TestTools.UnitTesting.ClassInitialize]
        public static void ClassInitialize(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext context)
        {
            AvaloniaTestHost.EnsureInitialized();
        }

        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void RtlWindow_DoesNotFlipTheKeypad()
        {
            var viewModel = new ApplicationViewModel();
            viewModel.Initialize(ViewMode.Standard);
            Window window = null;
            Dispatcher.UIThread.Post(() =>
            {
                window = new MainWindow { DataContext = viewModel };
                window.FlowDirection = global::Avalonia.Media.FlowDirection.RightToLeft;
                window.Show();
            });
            Dispatcher.UIThread.RunJobs();

            // Culture-driven RTL reaches the shell...
            var shell = window.GetVisualDescendants().OfType<Grid>().FirstOrDefault(g => g.Name == "AppShellRoot");
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(shell);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(
                global::Avalonia.Media.FlowDirection.RightToLeft, shell.FlowDirection);

            // ...without flipping the keypad (Avalonia 12 renders RTL text but
            // does not mirror the layout, and an explicit LTR would flip it).
            var numpad = window.GetVisualDescendants().OfType<Grid>().FirstOrDefault(g => g.Name == "StandardNumpad");
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(numpad);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(numpad.HasMirrorTransform,
                "The standard keypad must not be mirrored in RTL.");
        }
    }
}
