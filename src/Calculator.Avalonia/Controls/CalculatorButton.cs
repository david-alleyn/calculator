// Port of the UWP CalculatorButton: binds Command to the ViewModel's
// ButtonPressed command and carries the pressed operation as the
// command parameter.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using CalculatorApp.ViewModel.Common;

namespace CalculatorApp.Avalonia.Controls
{
    public class CalculatorButton : Button
    {
        public static readonly StyledProperty<string> AuditoryFeedbackProperty =
            AvaloniaProperty.Register<CalculatorButton, string>(nameof(AuditoryFeedback), defaultValue: string.Empty);

        public static readonly StyledProperty<NumbersAndOperatorsEnum> ButtonIdProperty =
            AvaloniaProperty.Register<CalculatorButton, NumbersAndOperatorsEnum>(nameof(ButtonId));

        public CalculatorButton()
        {
            this.Bind(CommandProperty, new Binding { Path = "ButtonPressed" });
        }

        public string AuditoryFeedback
        {
            get => GetValue(AuditoryFeedbackProperty);
            set => SetValue(AuditoryFeedbackProperty, value);
        }

        public NumbersAndOperatorsEnum ButtonId
        {
            get => GetValue(ButtonIdProperty);
            set => SetValue(ButtonIdProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == AuditoryFeedbackProperty || change.Property == ButtonIdProperty)
            {
                SetDefaultCommandParameter();
            }
        }

        protected override void OnDataContextChanged(System.EventArgs e)
        {
            base.OnDataContextChanged(e);
            SetDefaultCommandParameter();
        }

        // Mirrors the UWP control: unless the view set an explicit
        // CommandParameter (e.g. memory recall slots), the parameter carries
        // the ButtonId + narration feedback for the ViewModel.
        private void SetDefaultCommandParameter()
        {
            if (!IsSet(CommandParameterProperty))
            {
                CommandParameter = new CalculatorButtonPressedEventArgs(AuditoryFeedback, ButtonId);
            }
        }
    }
}
