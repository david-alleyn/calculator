// Phase 0 demo: an Avalonia window displaying a calculator expression,
// evaluated by the native CalcManager engine through the C ABI.

using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

internal static partial class CalculatorNative
{
    private const string LibraryName = "CalculatorNative";

    [LibraryImport(LibraryName)]
    internal static partial int calc_session_create(int mode, out nint session);

    [LibraryImport(LibraryName)]
    internal static partial void calc_session_destroy(nint session);

    [LibraryImport(LibraryName)]
    internal static partial int calc_session_send_command(nint session, uint commandId);

    [LibraryImport(LibraryName)]
    internal static partial nint calc_session_get_primary_display(nint session);

    [LibraryImport(LibraryName)]
    internal static partial nint calc_session_get_expression_display(nint session);
}

internal sealed class Program
{
    internal const uint Command0 = 130;
    internal const uint Command1 = 131;
    internal const uint Command2 = 132;
    internal const uint Command3 = 133;
    internal const uint Command4 = 134;
    internal const uint Command5 = 135;
    internal const uint Command6 = 136;
    internal const uint Command7 = 137;
    internal const uint Command8 = 138;
    internal const uint Command9 = 139;
    internal const uint CommandDIV = 91;
    internal const uint CommandMUL = 92;
    internal const uint CommandADD = 93;
    internal const uint CommandSUB = 94;
    internal const uint CommandEQU = 121;

    private const int CalModeStandard = 0;

    private static nint s_session;

    internal static TextBlock DisplayText;
    internal static TextBlock ExpressionText;

    private static string GetDisplay()
    {
        nint ptr = CalculatorNative.calc_session_get_primary_display(s_session);
        return ptr == nint.Zero ? string.Empty : Marshal.PtrToStringUTF8(ptr);
    }

    private static string GetExpression()
    {
        nint ptr = CalculatorNative.calc_session_get_expression_display(s_session);
        return ptr == nint.Zero ? string.Empty : Marshal.PtrToStringUTF8(ptr);
    }

    internal static void Send(uint command)
    {
        CalculatorNative.calc_session_send_command(s_session, command);
        DisplayText.Text = GetDisplay();
        ExpressionText.Text = GetExpression();
    }

    internal static Button MakeButton(string label, uint command, int row, int column)
    {
        var button = new Button
        {
            Content = label,
            FontSize = 18,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        button.Click += (_, _) => Program.Send(command);
        Grid.SetRow(button, row);
        Grid.SetColumn(button, column);
        return button;
    }

    [STAThread]
    private static int Main(string[] args)
    {
        int status = CalculatorNative.calc_session_create(CalModeStandard, out s_session);
        if (status != 0)
        {
            Console.Error.WriteLine("Failed to create engine session; is libCalculatorNative.so discoverable via LD_LIBRARY_PATH?");
            return 1;
        }

        return AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }
}

internal sealed class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindowView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class MainWindowView : Window
{
    public MainWindowView()
    {
        Title = "Calculator - Avalonia/Linux demo (Phase 0)";
        Width = 340;
        Height = 480;

        var grid = new Grid { Margin = new Avalonia.Thickness(12) };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (int i = 0; i < 6; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        }
        for (int i = 0; i < 4; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        Program.ExpressionText = new TextBlock
        {
            FontSize = 14,
            Foreground = Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Right,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Text = string.Empty,
        };
        Program.DisplayText = new TextBlock
        {
            FontSize = 40,
            HorizontalAlignment = HorizontalAlignment.Right,
            Text = "0",
        };

        Grid.SetRow(Program.ExpressionText, 0);
        Grid.SetRow(Program.DisplayText, 1);
        grid.Children.Add(Program.ExpressionText);
        grid.Children.Add(Program.DisplayText);

        grid.Children.Add(Program.MakeButton("%", 118, 2, 0));
        grid.Children.Add(Program.MakeButton("CE", 82, 2, 1));
        grid.Children.Add(Program.MakeButton("C", 81, 2, 2));
        grid.Children.Add(Program.MakeButton("⌫", 83, 2, 3));

        grid.Children.Add(Program.MakeButton("1/x", 114, 3, 0));
        grid.Children.Add(Program.MakeButton("x²", 111, 3, 1));
        grid.Children.Add(Program.MakeButton("√", 110, 3, 2));
        grid.Children.Add(Program.MakeButton("÷", Program.CommandDIV, 3, 3));

        grid.Children.Add(Program.MakeButton("7", Program.Command7, 4, 0));
        grid.Children.Add(Program.MakeButton("8", Program.Command8, 4, 1));
        grid.Children.Add(Program.MakeButton("9", Program.Command9, 4, 2));
        grid.Children.Add(Program.MakeButton("×", Program.CommandMUL, 4, 3));

        grid.Children.Add(Program.MakeButton("4", Program.Command4, 5, 0));
        grid.Children.Add(Program.MakeButton("5", Program.Command5, 5, 1));
        grid.Children.Add(Program.MakeButton("6", Program.Command6, 5, 2));
        grid.Children.Add(Program.MakeButton("-", Program.CommandSUB, 5, 3));

        grid.Children.Add(Program.MakeButton("1", Program.Command1, 6, 0));
        grid.Children.Add(Program.MakeButton("2", Program.Command2, 6, 1));
        grid.Children.Add(Program.MakeButton("3", Program.Command3, 6, 2));
        grid.Children.Add(Program.MakeButton("+", Program.CommandADD, 6, 3));

        grid.Children.Add(Program.MakeButton("±", 80, 7, 0));
        grid.Children.Add(Program.MakeButton("0", Program.Command0, 7, 1));
        grid.Children.Add(Program.MakeButton(".", 84, 7, 2));
        grid.Children.Add(Program.MakeButton("=", Program.CommandEQU, 7, 3));

        Content = grid;
    }
}
