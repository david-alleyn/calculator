// App shell code-behind: navigation pane toggle, mode selection, and
// always-on-top. Only Standard is selectable until Phases 3-4 land.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;

using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

namespace CalculatorApp.Avalonia.Views
{
    public partial class MainView : UserControl
    {
        private readonly ColumnDefinition _navColumn;

        public MainView()
        {
            InitializeComponent();

            _navColumn = ContentGrid.ColumnDefinitions[0];

            // Start with the navigation pane collapsed; the XAML reads
            // Width="0" but the fixed-width Border still renders until the
            // first toggle collapses it explicitly.
            SetNavPaneOpen(false);

            ApplyThemeBackground();
            if (Application.Current != null)
            {
                Application.Current.ActualThemeVariantChanged += (_, _) => ApplyThemeBackground();
            }

            AlwaysOnTopButton.SetValue(ToolTip.TipProperty,
                AppResourceProvider.GetInstance().GetResourceString("EnterAlwaysOnTopButton.[using:Windows.UI.Xaml.Controls]ToolTipService.ToolTip"));
        }

        public ApplicationViewModel ViewModel => DataContext as ApplicationViewModel;

        // Avalonia 12 theme dictionaries did not switch our custom background
        // brush across variants, so resolve it explicitly here.
        private void ApplyThemeBackground()
        {
            bool dark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            IBrush brush = Application.Current?.TryGetResource(
                dark ? "AppWindowBackgroundDarkBrush" : "AppWindowBackgroundLightBrush",
                Application.Current.ActualThemeVariant,
                out object resolved) == true && resolved is IBrush resolvedBrush
                ? resolvedBrush
                : null;

            if (brush != null)
            {
                AppShellRoot.Background = brush;
            }
        }

        private ApplicationViewModel _attachedViewModel;

        protected override void OnDataContextChanged(System.EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (_attachedViewModel != null)
            {
                _attachedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _attachedViewModel = ViewModel;
            if (_attachedViewModel != null)
            {
                _attachedViewModel.PropertyChanged += OnViewModelPropertyChanged;
            }

            LimitSelectableModes();
            UpdateModeVisibility();
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ApplicationViewModel.Mode))
            {
                UpdateModeVisibility();
            }
        }

        // Date and Converter use different ViewModels and views, so the shell
        // swaps the content view instead of keypads within CalculatorView.
        private void UpdateModeVisibility()
        {
            ViewMode mode = ViewModel?.Mode ?? ViewMode.None;
            bool isDate = mode == ViewMode.Date;
            bool isConverter = NavCategory.IsConverterViewMode(mode);

            Calculator.IsVisible = !isDate && !isConverter;
            DateCalculator.IsVisible = isDate;
            UnitConverter.IsVisible = isConverter;
        }

        // Only the ported views are selectable; keep the rest of the
        // navigation visible but disabled until the remaining modes land.
        private static readonly ViewMode[] s_supportedModes =
        {
            ViewMode.Standard,
            ViewMode.Scientific,
            ViewMode.Programmer,
            ViewMode.Date,
        };

        private void LimitSelectableModes()
        {
            if (ViewModel == null)
            {
                return;
            }

            foreach (NavCategoryGroup group in ViewModel.Categories)
            {
                foreach (NavCategory category in group.Categories)
                {
                    bool supported = System.Array.IndexOf(s_supportedModes, category.ViewMode) >= 0
                        || NavCategory.IsConverterViewMode(category.ViewMode);

                    if (!supported)
                    {
                        category.IsEnabled = false;
                    }
                }
            }
        }

        private void ToggleNavPane(object sender, RoutedEventArgs e)
        {
            bool open = NavPane.IsVisible;
            SetNavPaneOpen(!open);
        }

        private void OnNavSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is NavCategory selected)
            {
                ViewModel.Mode = selected.ViewMode;
                listBox.SelectedItem = null;
                SetNavPaneOpen(false);
            }
        }

        // The pane is a fixed-width Border; collapsing the grid column alone
        // leaves it overflowing (Grid does not clip), so toggle the pane's own
        // visibility too.
        private void SetNavPaneOpen(bool open)
        {
            NavPane.IsVisible = open;
            _navColumn.Width = open ? new GridLength(256) : new GridLength(0);

            // Hide the always-on-top affordance while the pane is open, like
            // the UWP NavigationView pane behavior.
            AlwaysOnTopButton.IsVisible = !open;
        }

        private void ToggleAlwaysOnTop(object sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.Topmost = !window.Topmost;
            }
        }
    }
}
