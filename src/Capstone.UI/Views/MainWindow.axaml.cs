using Avalonia.Controls;
using Avalonia.Interactivity;
using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;
using Capstone.Core.ViewModels;
using Capstone.Core.Web;

namespace Capstone.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private CatalogueViewModel? Vm => DataContext as CatalogueViewModel;

    // ── Add Manual ────────────────────────────────────────────────────────────

    private async void OnAddManualClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new AddGameDialog();
        var result = await dialog.ShowDialog<DigitalGame?>(this);
        if (result is not null)
            Vm?.AddCommand.Execute(result);
    }

    // ── Add from IGDB ─────────────────────────────────────────────────────────

    private async void OnAddIgdbClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new IgdbSearchDialog();
        var result = await dialog.ShowDialog<DigitalGame?>(this);
        if (result is not null)
            Vm?.AddCommand.Execute(result);
    }
}
