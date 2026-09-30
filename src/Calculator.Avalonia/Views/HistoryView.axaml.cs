// History pane code-behind. Wires item taps and the per-item context menu
// to HistoryViewModel.ShowItem / DeleteItem, and localizes static strings.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

namespace CalculatorApp.Avalonia.Views
{
    public partial class HistoryView : UserControl
    {
        public HistoryView()
        {
            InitializeComponent();

            var resources = AppResourceProvider.GetInstance();
            HistoryLabel.Text = resources.GetResourceString("HistoryLabel.Text");
            HistoryEmptyText.Text = resources.GetResourceString("HistoryEmpty.Text");
            ClearHistoryButton.SetValue(ToolTip.TipProperty, resources.GetResourceString("ClearHistory.[using:Windows.UI.Xaml.Controls]ToolTipService.ToolTip"));
        }

        private HistoryViewModel ViewModel => DataContext as HistoryViewModel;

        private void OnItemTapped(object sender, TappedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            if (e.Source is StyledElement source && FindDataContext<HistoryItemViewModel>(source) is HistoryItemViewModel item)
            {
                ViewModel.ShowItem(item);
            }
        }

        private void OnCopyItemClicked(object sender, RoutedEventArgs e)
        {
            // Avalonia's MenuFlyout propagates the target's DataContext to the presenter.
            if ((sender as MenuItem)?.DataContext is HistoryItemViewModel item)
            {
                CopyPasteManager.CopyToClipboard(item.Result);
            }
        }

        private void OnDeleteItemClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && (sender as MenuItem)?.DataContext is HistoryItemViewModel item)
            {
                ViewModel.DeleteItem(item);
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
