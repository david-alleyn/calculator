// Scientific operators code-behind: shift (inverse) row toggling and the
// trigonometry flyout's shift/hyp toggles (port of
// CalculatorScientificOperators.xaml.cs).

using Avalonia.Controls;
using Avalonia.Interactivity;

using CalculatorApp.ViewModel;

namespace CalculatorApp.Avalonia.Views
{
    public partial class ScientificOperatorsView : UserControl
    {
        public ScientificOperatorsView()
        {
            InitializeComponent();
        }

        public StandardCalculatorViewModel ViewModel => DataContext as StandardCalculatorViewModel;

        // Main-panel shift: swaps the advanced function column between the
        // direct (x², √, …) and inverse (x³, ∛, …) rows.
        private void ShiftButton_Check(object sender, RoutedEventArgs e)
        {
            SetOperatorRowVisibility();
        }

        private void SetOperatorRowVisibility()
        {
            bool inverse = ShiftButton.IsChecked == true;
            Row1.IsVisible = !inverse;
            InvRow1.IsVisible = inverse;
        }

        private void TrigToggle_Toggle(object sender, RoutedEventArgs e)
        {
            SetTrigRowVisibility();
        }

        private void SetTrigRowVisibility()
        {
            bool shift = TrigShiftButton.IsChecked == true;
            bool hyp = HypButton.IsChecked == true;

            TrigFunctions.IsVisible = !shift && !hyp;
            InverseTrigFunctions.IsVisible = shift && !hyp;
            HyperbolicTrigFunctions.IsVisible = !shift && hyp;
            InverseHyperbolicTrigFunctions.IsVisible = shift && hyp;
        }

        // Closes the operator flyout and clears its toggles after a function
        // is chosen, matching the UWP FlyoutButton_Clicked behavior.
        private void FlyoutButton_Clicked(object sender, RoutedEventArgs e)
        {
            HypButton.IsChecked = false;
            TrigShiftButton.IsChecked = false;
            SetTrigRowVisibility();

            TrigButton.Flyout?.Hide();
            FuncButton.Flyout?.Hide();
        }
    }
}
