// View-layer smoke tests for Programmer mode: mode switching, radix display,
// hex input, the bit-flip panel, and word-size cycling, all through the live
// native engine.

using System.Linq;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CalculatorApp.Avalonia;
using CalculatorApp.Avalonia.Controls;
using CalculatorApp.Avalonia.Views;
using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Calculator.Avalonia.Tests
{
    [TestClass]
    public class ProgrammerCalculatorViewTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            AvaloniaTestHost.EnsureInitialized();
        }

        private static (Window window, ApplicationViewModel viewModel) OpenCalculator()
        {
            var viewModel = new ApplicationViewModel();
            viewModel.Initialize(ViewMode.Programmer);

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

        private static ProgrammerDisplayView GetDisplayPanel(Window window)
        {
            var panel = FindIn<ProgrammerDisplayView>(window, "ProgrammerDisplay");
            Assert.IsNotNull(panel, "Expected the programmer display panel in the view tree.");
            Assert.IsTrue(panel.IsVisible, "Programmer mode should show the programmer display panel.");
            return panel;
        }

        private static ProgrammerOperatorsView GetKeypad(Window window)
        {
            var keypad = FindIn<ProgrammerOperatorsView>(window, "ProgrammerOperators");
            Assert.IsNotNull(keypad, "Expected the programmer keypad in the view tree.");
            Assert.IsTrue(keypad.IsVisible, "Programmer mode should show the radix keypad by default.");
            return keypad;
        }

        private static void PressButton(Control root, string buttonName)
        {
            var button = FindIn<CalculatorButton>(root, buttonName);
            Assert.IsNotNull(button, $"Expected a button named {buttonName}.");
            Assert.IsNotNull(button.Command, $"Button {buttonName} has no command (DataContext wiring broken).");
            button.Command.Execute(button.CommandParameter);
        }

        [TestMethod]
        public void SwitchingToProgrammer_ShowsProgrammerPanels_HidesMemory()
        {
            var (window, _) = OpenCalculator();

            GetDisplayPanel(window);
            GetKeypad(window);

            var memory = FindIn<Grid>(window, "MemoryPanel");
            Assert.IsNotNull(memory);
            Assert.IsFalse(memory.IsVisible, "The memory row should be hidden in Programmer mode.");

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ProgrammerMode_HexInput_ConvertsAcrossRadixes()
        {
            var (window, viewModel) = OpenCalculator();
            var display = GetDisplayPanel(window);
            var keypad = GetKeypad(window);
            var calculator = viewModel.CalculatorViewModel;

            // Select the hexadecimal radix.
            var hexButton = FindIn<RadioButton>(display, "HexButton");
            Assert.IsNotNull(hexButton);
            hexButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.AreEqual(NumberBase.HexBase, calculator.CurrentRadixType);

            PressButton(keypad, "AButton");

            Assert.AreEqual("A", calculator.HexDisplayValue);
            Assert.AreEqual("10", calculator.DecimalDisplayValue);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ProgrammerMode_BitFlip_TogglesBit()
        {
            var (window, viewModel) = OpenCalculator();
            var display = GetDisplayPanel(window);
            var calculator = viewModel.CalculatorViewModel;

            var bitFlipButton = FindIn<RadioButton>(display, "BitFlipButton");
            Assert.IsNotNull(bitFlipButton);
            bitFlipButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.IsTrue(calculator.IsBitFlipChecked);
            var bitFlipPanel = FindIn<ProgrammerBitFlipView>(window, "ProgrammerBitFlip");
            Assert.IsNotNull(bitFlipPanel);
            Assert.IsTrue(bitFlipPanel.IsVisible, "The bit-flip panel should be visible.");

            // Set bit 1 from the panel.
            var bit1 = bitFlipPanel.GetVisualDescendants()
                .OfType<ToggleButton>()
                .FirstOrDefault(toggle => toggle.Tag is int index && index == 1);
            Assert.IsNotNull(bit1, "Expected a toggle for bit 1.");

            bit1.IsChecked = true;
            bit1.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.AreEqual("2", calculator.DecimalDisplayValue);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ProgrammerMode_WordSize_Cycles()
        {
            var (window, viewModel) = OpenCalculator();
            var display = GetDisplayPanel(window);
            var calculator = viewModel.CalculatorViewModel;

            Assert.AreEqual(BitLength.BitLengthQWord, calculator.ValueBitLength);

            var wordSize = FindIn<Button>(display, "WordSizeButton");
            Assert.IsNotNull(wordSize);
            wordSize.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.AreEqual(BitLength.BitLengthDWord, calculator.ValueBitLength);

            wordSize.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.AreEqual(BitLength.BitLengthWord, calculator.ValueBitLength);

            wordSize.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.AreEqual(BitLength.BitLengthByte, calculator.ValueBitLength);

            wordSize.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.AreEqual(BitLength.BitLengthQWord, calculator.ValueBitLength);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ProgrammerMode_AndOperator_Computes()
        {
            var (window, viewModel) = OpenCalculator();
            var keypad = GetKeypad(window);
            var calculator = viewModel.CalculatorViewModel;

            PressButton(keypad, "Num1Button");

            var bitwiseButton = FindIn<Button>(keypad, "BitwiseButton");
            Assert.IsNotNull(bitwiseButton?.Flyout);
            bitwiseButton.Flyout.ShowAt(bitwiseButton);
            Dispatcher.UIThread.RunJobs();

            var flyoutContent = (bitwiseButton.Flyout as Flyout)?.Content as Control;
            Assert.IsNotNull(flyoutContent);

            var and = FindIn<CalculatorButton>(flyoutContent, "AndButton");
            Assert.IsNotNull(and, "Expected the AND button in the bitwise flyout.");
            Assert.IsNotNull(and.Command, "Flyout buttons should inherit the ViewModel DataContext.");
            and.Command.Execute(and.CommandParameter);

            PressButton(keypad, "Num3Button");
            PressButton(keypad, "EqualButton");

            Assert.AreEqual("1", calculator.DecimalDisplayValue);

            Dispatcher.UIThread.Post(window.Close);
        }
    }
}
