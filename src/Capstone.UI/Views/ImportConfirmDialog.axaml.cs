using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Capstone.UI.Views;

public partial class ImportConfirmDialog : Window
{
    // Parameterless ctor required by Avalonia's XAML loader (design-time / previewer).
    public ImportConfirmDialog() : this(0, 0) { }

    public ImportConfirmDialog(int currentCount, int importCount)
    {
        InitializeComponent();

        BodyText.Text =
            $"Your library currently has {currentCount} game{(currentCount == 1 ? "" : "s")}. " +
            $"Importing will replace it with {importCount} game{(importCount == 1 ? "" : "s")} from the file.";
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e) =>
        Close(true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) =>
        Close(false);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close(false);
        }
    }
}
