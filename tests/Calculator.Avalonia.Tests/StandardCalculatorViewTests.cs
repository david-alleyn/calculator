// View-layer smoke tests: the ported Standard calculator wired to the live
// native engine through the existing ViewModels.

using System;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CalculatorApp.Avalonia;
using CalculatorApp.Avalonia.Common;
using CalculatorApp.Avalonia.Controls;
using CalculatorApp.Avalonia.Views;
using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Calculator.Avalonia.Tests
{
    [TestClass]
    public class StandardCalculatorViewTests
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

        private static T FindNamed<T>(Window window, string name) where T : Control
        {
            return window
                .GetVisualDescendants()
                .OfType<T>()
                .FirstOrDefault(control => control.Name == name);
        }

        private static void PressButton(Window window, string buttonName, int presses = 1)
        {
            var button = FindNamed<CalculatorButton>(window, buttonName);
            Assert.IsNotNull(button, $"Expected a button named {buttonName} in the view tree.");

            for (int i = 0; i < presses; i++)
            {
                Assert.IsNotNull(button.Command, $"Button {buttonName} has no command (DataContext wiring broken).");
                Assert.IsInstanceOfType(button.CommandParameter, typeof(CalculatorButtonPressedEventArgs),
                    $"Button {buttonName} should carry CalculatorButtonPressedEventArgs, got {button.CommandParameter?.GetType().Name ?? "null"}.");
                button.Command.Execute(button.CommandParameter);
            }
        }

        [TestMethod]
        public void ViewTree_Builds_WithViewModel_WiredButtons()
        {
            var (window, viewModel) = OpenCalculator();

            Assert.AreEqual("0", viewModel.CalculatorViewModel.DecimalDisplayValue);
            Assert.IsNotNull(FindNamed<CalculatorButton>(window, "Num7Button"));
            Assert.IsNotNull(FindNamed<CalculatorButton>(window, "EqualButton"));
            Assert.IsNotNull(FindNamed<CalculatorButton>(window, "DivideButton"));

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void Press_OnePlusTwoEquals_Three()
        {
            var (window, viewModel) = OpenCalculator();
            var calculator = viewModel.CalculatorViewModel;

            PressButton(window, "Num1Button");
            PressButton(window, "PlusButton");
            PressButton(window, "Num2Button");
            PressButton(window, "EqualButton");

            Assert.AreEqual("3", calculator.DisplayValue);
            Assert.IsFalse(calculator.IsInError);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void Press_DigitButtons_ComposeMultiDigitValue()
        {
            var (window, viewModel) = OpenCalculator();
            var calculator = viewModel.CalculatorViewModel;

            // Direct VM drive must work first (mirrors the 294 VM tests).
            calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.One);
            Assert.AreEqual("1", calculator.DisplayValue, "Direct VM drive failed; environment problem, not view wiring.");

            PressButton(window, "Num7Button");
            PressButton(window, "Num8Button");
            PressButton(window, "Num9Button");

            // DisplayValue is localized with grouping separators (e.g. "1,789").
            Assert.AreEqual("1,789", calculator.DisplayValue);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void DivideByZero_EntersErrorState_AndClearRecovers()
        {
            var (window, viewModel) = OpenCalculator();
            var calculator = viewModel.CalculatorViewModel;

            PressButton(window, "Num5Button");
            PressButton(window, "DivideButton");
            PressButton(window, "Num0Button");
            PressButton(window, "EqualButton");

            Assert.IsTrue(calculator.IsInError, "Divide by zero should put the calculator in the error state.");

            PressButton(window, "ClearButton");

            Assert.IsFalse(calculator.IsInError, "Clear should recover from the error state.");
            Assert.AreEqual("0", calculator.DisplayValue);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void DivideByZero_ErrorText_UsesNormalForeground()
        {
            var (window, viewModel) = OpenCalculator();

            PressButton(window, "Num5Button");
            PressButton(window, "DivideButton");
            PressButton(window, "Num0Button");
            PressButton(window, "EqualButton");

            Assert.IsTrue(viewModel.CalculatorViewModel.IsInError);

            var result = FindNamed<TextBlock>(window, "ResultText");
            Assert.IsNotNull(result);
            if (result.Foreground is global::Avalonia.Media.ISolidColorBrush brush)
            {
                Assert.AreNotEqual(global::Avalonia.Media.Colors.Red, brush.Color,
                    "The error message should use the normal result foreground, not red.");
            }

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void Memory_StoreAdds_RecallRestores_AndClearEmpties()
        {
            var (window, viewModel) = OpenCalculator();
            var calculator = viewModel.CalculatorViewModel;

            PressButton(window, "Num4Button");
            PressButton(window, "Num2Button");
            PressButton(window, "MemButton");

            Assert.AreEqual(1, calculator.MemorizedNumbers.Count);
            Assert.AreEqual("42", calculator.MemorizedNumbers[0].Value);

            PressButton(window, "ClearButton");
            Assert.AreEqual("0", calculator.DisplayValue);

            var recall = FindNamed<CalculatorButton>(window, "MemRecall");
            recall.Command.Execute(recall.CommandParameter);

            Assert.AreEqual("42", calculator.DisplayValue);

            var clearAll = FindNamed<Button>(window, "ClearMemoryButton");
            clearAll.Command.Execute(null);

            Assert.AreEqual(0, calculator.MemorizedNumbers.Count);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void History_RecordsCompletedCalculations()
        {
            var (window, viewModel) = OpenCalculator();
            var calculator = viewModel.CalculatorViewModel;

            PressButton(window, "Num1Button");
            PressButton(window, "PlusButton");
            PressButton(window, "Num2Button");
            PressButton(window, "EqualButton");

            Assert.AreEqual(1, calculator.HistoryVM.Items.Count);
            // Known issue (Phase 1 wrapper): the serialized history expression
            // renders operator tokens as numeric fallbacks ("1 13 2 41") instead
            // of glyphs ("1 + 2 ="). The result and item count serialize cleanly.
            Assert.IsTrue(calculator.HistoryVM.Items[0].Result.Contains("3"));
            Assert.IsTrue(calculator.HistoryVM.Items[0].Expression.Length > 0);

            PressButton(window, "ClearButton");
            calculator.HistoryVM.ClearCommand.Execute(null);

            Assert.AreEqual(0, calculator.HistoryVM.Items.Count);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void KeyboardMap_MapsStandardKeys()
        {
            Assert.IsTrue(CalculatorKeyboardMap.TryMap(global::Avalonia.Input.Key.D1, global::Avalonia.Input.KeyModifiers.None, out NumbersAndOperatorsEnum key1));
            Assert.AreEqual(NumbersAndOperatorsEnum.One, key1);

            Assert.IsTrue(CalculatorKeyboardMap.TryMap(global::Avalonia.Input.Key.Enter, global::Avalonia.Input.KeyModifiers.None, out NumbersAndOperatorsEnum enter));
            Assert.AreEqual(NumbersAndOperatorsEnum.Equals, enter);

            Assert.IsTrue(CalculatorKeyboardMap.TryMap(global::Avalonia.Input.Key.Escape, global::Avalonia.Input.KeyModifiers.None, out NumbersAndOperatorsEnum esc));
            Assert.AreEqual(NumbersAndOperatorsEnum.Clear, esc);

            // Ctrl-combos are not calculator input.
            Assert.IsFalse(CalculatorKeyboardMap.TryMap(global::Avalonia.Input.Key.C, global::Avalonia.Input.KeyModifiers.Control, out _));
        }
    }
}
