// Memory pane code-behind. DataContext is StandardCalculatorViewModel; taps
// on items recall them, and the per-item MC/M+/M- buttons drive
// MemoryItemViewModel.Clear / MemoryAdd / MemorySubtract.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

namespace CalculatorApp.Avalonia.Views
{
    public partial class MemoryView : UserControl
    {
        public MemoryView()
        {
            InitializeComponent();

            var resources = AppResourceProvider.GetInstance();
            MemoryLabel.Text = resources.GetResourceString("MemoryLabel.Text");
            MemoryPaneEmptyText.Text = resources.GetResourceString("MemoryPaneEmpty.Text");
        }

        private StandardCalculatorViewModel ViewModel => DataContext as StandardCalculatorViewModel;

        private void OnItemTapped(object sender, TappedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            if (e.Source is StyledElement source && FindDataContext<MemoryItemViewModel>(source) is MemoryItemViewModel item)
            {
                ViewModel.MemoryItemPressedCommand.Execute(item.Position);
            }
        }

        private void OnClearItemTapped(object sender, TappedEventArgs e)
        {
            e.Handled = true;

            if (e.Source is StyledElement source && FindDataContext<MemoryItemViewModel>(source) is MemoryItemViewModel item)
            {
                item.Clear();
            }
        }

        private void OnAddItemTapped(object sender, TappedEventArgs e)
        {
            e.Handled = true;

            if (e.Source is StyledElement source && FindDataContext<MemoryItemViewModel>(source) is MemoryItemViewModel item)
            {
                item.MemoryAdd();
            }
        }

        private void OnSubtractItemTapped(object sender, TappedEventArgs e)
        {
            e.Handled = true;

            if (e.Source is StyledElement source && FindDataContext<MemoryItemViewModel>(source) is MemoryItemViewModel item)
            {
                item.MemorySubtract();
            }
        }

        private static T FindDataContext<T>(StyledElement source) where T : class
        {
            for (StyledElement current = source; current != null; current = current.Parent)
            {
                if (current is Control control && control.DataContext is T typed)
                {
                    return typed;
                }
            }

            return null;
        }
    }
}
