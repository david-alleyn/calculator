// Calculator view code-behind: expression/result rendering, docked panel
// vs full-width flyout switching, and error-state fonts.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

namespace CalculatorApp.Avalonia.Views
{
    public partial class CalculatorView : UserControl
    {
        private const double DockTriggerWidth = 560;
        private const double DockPaneWidth = 320;

        private readonly ColumnDefinition _dockColumn;
        private readonly ColumnDefinition _memoryButtonColumn;
        private bool _docked;
        private string _expressionText = string.Empty;

        public CalculatorView()
        {
            InitializeComponent();

            HistoryPopup.PlacementTarget = this;
            MemoryPopup.PlacementTarget = this;
            HistoryPopup.CustomPopupPlacementCallback = PopupPlacementCallback;
            MemoryPopup.CustomPopupPlacementCallback = PopupPlacementCallback;

            UpdateFlyoutChrome();
            if (Application.Current != null)
            {
                Application.Current.ActualThemeVariantChanged += (_, _) => UpdateFlyoutChrome();
            }

            // Named column definitions do not surface to code-behind fields in
            // Avalonia; resolve them structurally.
            _dockColumn = RootGrid.ColumnDefinitions[1];
            _memoryButtonColumn = MemoryPanel.ColumnDefinitions[5];

            // When in the flyout (custom popup placement), Avalonia does not hook
            // closing on outside clicks; close both popups together like UWP's
            // full flyouts, and remember focus for the results text.
            HistoryPopup.Opened += (_, _) =>
            {
                HistoryButton.SetValue(ToolTip.TipProperty, AppResourceProvider.GetInstance().GetResourceString("HistoryButton_Close"));
            };
            HistoryPopup.Closed += (_, _) => FocusResults();
            MemoryPopup.Closed += (_, _) => FocusResults();

            var resources = AppResourceProvider.GetInstance();
            DockHistoryHeader.Text = resources.GetResourceString("HistoryLabel.Text");
            DockMemoryHeader.Text = resources.GetResourceString("MemoryLabel.Text");
            HistoryButton.SetValue(ToolTip.TipProperty, resources.GetResourceString("HistoryButton_Open"));

            SizeChanged += OnSizeChanged;
        }

        private StandardCalculatorViewModel ViewModel => DataContext as StandardCalculatorViewModel;

        private void UpdateFlyoutChrome()
        {
            bool dark = Application.Current?.ActualThemeVariant == global::Avalonia.Styling.ThemeVariant.Dark;
            string key = dark ? "AppWindowBackgroundDarkBrush" : "AppWindowBackgroundLightBrush";
            if (Application.Current?.TryGetResource(key, Application.Current.ActualThemeVariant, out object resolved) == true
                && resolved is global::Avalonia.Media.IBrush brush)
            {
                HistoryFlyoutHost.Background = brush;
                MemoryFlyoutHost.Background = brush;
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            // Popup content does not inherit DataContext (separate visual root).
            HistoryFlyoutView.DataContext = ViewModel?.HistoryVM;
            MemoryFlyoutView.DataContext = ViewModel;

            if (ViewModel != null && !_subscribed)
            {
                _subscribed = true;
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
                ViewModel.ExpressionTokens.CollectionChanged += (_, _) => UpdateExpression();
                ViewModel.HistoryVM.HistoryItemClicked += OnHistoryItemClicked;
                ViewModel.HistoryVM.HideHistoryClicked += OnHideHistoryClicked;
                ViewModel.HideMemoryClicked += OnHideMemoryClicked;
            }

            ResetDisplay();
            UpdatePanels();
        }

        private bool _subscribed;

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePanels();
            UpdateResultFont();
        }

        private void UpdatePanels()
        {
            double width = Bounds.Width;
            bool docked = width >= DockTriggerWidth;

            if (docked == _docked && width > 0)
            {
                return;
            }
            _docked = docked;

            _dockColumn.Width = docked ? new GridLength(DockPaneWidth) : new GridLength(0);
            DockPane.IsVisible = docked;

            // The toolbar history button and the memory-row flyout button only
            // make sense when the docked panel is collapsed.
            HistoryButton.IsVisible = !docked;
            MemoryFlyoutButton.IsVisible = !docked;
            _memoryButtonColumn.Width = docked ? new GridLength(0) : new GridLength(1, GridUnitType.Star);

            if (docked)
            {
                CloseFlyouts();
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(StandardCalculatorViewModel.IsInError):
                    ResetDisplay();
                    break;
                case StandardCalculatorViewModel.IsMemoryEmptyPropertyName:
                    UpdateMemoryCaptions();
                    break;
                case nameof(StandardCalculatorViewModel.DisplayValue):
                case nameof(StandardCalculatorViewModel.IsOperatorCommand):
                    ResultText.Text = ViewModel?.DisplayValue ?? "0";
                    UpdateResultFont();
                    break;
                case nameof(StandardCalculatorViewModel.AreTokensUpdated):
                    UpdateExpression();
                    break;
            }
        }

        private void ResetDisplay()
        {
            if (ViewModel == null)
            {
                ResultText.Text = "0";
                ExpressionText.Text = string.Empty;
                return;
            }

            ResultText.Text = ViewModel.DisplayValue;
            ResultText.Foreground = ViewModel.IsInError
                ? Brushes.Red
                : (IBrush)Application.Current.FindResource("SystemControlForegroundBaseHighBrush");
            UpdateExpression();
            UpdateMemoryCaptions();
            UpdateResultFont();
        }

        private void UpdateMemoryCaptions()
        {
            bool memoryEnabled = ViewModel is { IsInError: false, IsMemoryEmpty: false };
            ClearMemoryButton.IsEnabled = memoryEnabled;
            MemRecall.IsEnabled = memoryEnabled;
            MemPlus.IsEnabled = memoryEnabled;
            MemMinus.IsEnabled = memoryEnabled;
        }

        private void UpdateExpression()
        {
            if (ViewModel == null)
            {
                ExpressionText.Text = string.Empty;
                return;
            }

            string joined = string.Join(string.Empty, ViewModel.ExpressionTokens.Select(t => t.Token));
            if (joined != _expressionText)
            {
                _expressionText = joined;
                ExpressionText.Text = joined;
                // Keep the newest tokens visible (right edge).
                Dispatcher.Post(() => ExpressionScroller.ScrollToEnd(), DispatcherPriority.Background);
            }
        }

        private void UpdateResultFont()
        {
            double width = Bounds.Width;
            if (width <= double.Epsilon)
            {
                width = 300;
            }

            double height = Bounds.Height;
            double maxSize = height >= 800 ? 72 : height >= 640 ? 46 : 26;

            ResultText.FontSize = FitResultFontSize(ResultText.Text ?? "0", Math.Max(12, maxSize), width - 24);
        }

        private double FitResultFontSize(string text, double maxSize, double availableWidth)
        {
            double size = maxSize;
            Typeface typeface = new Typeface("Segoe UI, sans-serif", FontStyle.Normal, FontWeight.SemiBold);

            while (size > 12)
            {
                var formatted = new FormattedText(
                    text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    size,
                    null);
                if (formatted.Width <= availableWidth)
                {
                    break;
                }
                size -= 1;
            }

            return size;
        }

        private void ToggleHistoryFlyout(object sender, RoutedEventArgs e)
        {
            ToggleHistoryPanel();
        }

        // Ctrl+H equivalent: the docked pane is always visible when wide, so
        // this toggles the full-width flyout (narrow windows).
        public void ToggleHistoryPanel()
        {
            if (_docked)
            {
                return;
            }

            if (HistoryPopup.IsOpen)
            {
                CloseFlyouts();
                return;
            }

            CloseFlyouts();
            PrepareFlyoutPopup(HistoryPopup);
            HistoryPopup.IsOpen = true;
        }

        private void ToggleMemoryFlyout(object sender, RoutedEventArgs e)
        {
            if (MemoryPopup.IsOpen)
            {
                CloseFlyouts();
                return;
            }

            CloseFlyouts();
            PrepareFlyoutPopup(MemoryPopup);
            MemoryPopup.IsOpen = true;
        }

        private void CloseFlyouts()
        {
            HistoryPopup.IsOpen = false;
            MemoryPopup.IsOpen = false;
        }

        private void OnHideHistoryClicked()
        {
            CloseFlyouts();
        }

        private void OnHideMemoryClicked()
        {
            CloseFlyouts();
        }

        private void OnHistoryItemClicked(HistoryItemViewModel item)
        {
            ViewModel?.SelectHistoryItem(item);
            CloseFlyouts();
        }

        private void FocusResults()
        {
            ResultText.Focus();
        }

        // The popup covers the calculator area (matching UWP's full flyouts);
        // the popup's Width/Height are set from the control bounds before it opens.
        private static void PopupPlacementCallback(CustomPopupPlacement placement)
        {
            placement.Anchor = PopupAnchor.Top;
            placement.Gravity = PopupGravity.Top;
            placement.Offset = default(Point);
            placement.ConstraintAdjustment = PopupPositionerConstraintAdjustment.None;
        }

        private void PrepareFlyoutPopup(Popup popup)
        {
            popup.Width = Bounds.Width;
            popup.Height = Bounds.Height;
            popup.HorizontalOffset = 0;
            popup.VerticalOffset = 0;
        }
    }
}
