// Programmer radix operators code-behind: bit-shift mode selection and flyout
// dismissal (port of CalculatorProgrammerRadixOperators.xaml.cs).

using Avalonia.Controls;
using Avalonia.Interactivity;

using CalculatorApp.ViewModel;

namespace CalculatorApp.Avalonia.Views
{
    public partial class ProgrammerOperatorsView : UserControl
    {
        public ProgrammerOperatorsView()
        {
            InitializeComponent();
        }

        public StandardCalculatorViewModel ViewModel => DataContext as StandardCalculatorViewModel;

        private void FlyoutButton_Clicked(object sender, RoutedEventArgs e)
        {
            BitwiseButton.Flyout?.Hide();
        }

        private void ShiftMode_Click(object sender, RoutedEventArgs e)
        {
            SetShiftMode((RadioButton)sender);

            BitShiftButton.Flyout?.Hide();
        }

        // Only one left/right shift pair is visible at a time, matching the
        // UWP bitshift flyout selection.
        private void SetShiftMode(RadioButton selected)
        {
            bool arithmetic = selected == ArithmeticShiftButton;
            bool logical = selected == LogicalShiftButton;
            bool rotateCircular = selected == RotateCircularButton;
            bool rotateCarry = selected == RotateCarryShiftButton;

            LshButton.IsVisible = arithmetic;
            RshButton.IsVisible = arithmetic;
            LshLogicalButton.IsVisible = logical;
            RshLogicalButton.IsVisible = logical;
            RolButton.IsVisible = rotateCircular;
            RorButton.IsVisible = rotateCircular;
            RolCarryButton.IsVisible = rotateCarry;
            RorCarryButton.IsVisible = rotateCarry;
        }
    }
}
