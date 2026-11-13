using System.Collections.Specialized;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;

namespace Capstone.UI.DevConsole;

public partial class DevConsoleView : UserControl
{
    // ── Brush converter (static, referenced from AXAML) ──────────────────────

    public static readonly IValueConverter KindBrushConverter = new LineKindConverter();

    private sealed class LineKindConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is LineKind kind ? KindToBrush(kind) : Brushes.White;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();

        private static IBrush KindToBrush(LineKind kind) => kind switch
        {
            LineKind.Input  => new SolidColorBrush(Color.Parse("#a6e3a1")), // green
            LineKind.Output => new SolidColorBrush(Color.Parse("#cdd6f4")), // text
            LineKind.System => new SolidColorBrush(Color.Parse("#89b4fa")), // blue
            LineKind.Error  => new SolidColorBrush(Color.Parse("#f38ba8")), // red
            _               => Brushes.White
        };
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    public DevConsoleView()
    {
        InitializeComponent();

        // Auto-scroll when new lines are added.
        DataContextChanged += (_, _) => SubscribeToLines();
    }

    private void SubscribeToLines()
    {
        if (DataContext is DevConsoleViewModel vm)
        {
            vm.Lines.CollectionChanged += OnLinesChanged;
        }
    }

    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Scroll to the bottom whenever a line is added.
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            LogScroll.ScrollToEnd();
        }, Avalonia.Threading.DispatcherPriority.Background);
    }

    // ── Event handlers ───────────────────────────────────────────────────────

    private void OnRun(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as DevConsoleViewModel)?.Execute();
        InputBox.Focus();
    }

    private void OnClose(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is DevConsoleViewModel vm)
            vm.IsVisible = false;
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not DevConsoleViewModel vm) return;

        switch (e.Key)
        {
            case Key.Enter:
                vm.Execute();
                e.Handled = true;
                break;
            case Key.Up:
                vm.HistoryUp();
                // Move caret to end after history navigation.
                InputBox.CaretIndex = InputBox.Text?.Length ?? 0;
                e.Handled = true;
                break;
            case Key.Down:
                vm.HistoryDown();
                InputBox.CaretIndex = InputBox.Text?.Length ?? 0;
                e.Handled = true;
                break;
        }
    }

    // ── Public focus helper ───────────────────────────────────────────────────

    public void FocusInput() => InputBox.Focus();
}

