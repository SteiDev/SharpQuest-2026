using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;
using Capstone.Core.ViewModels;
using Capstone.Core.Web;
using Capstone.UI.DevConsole;
using SharpQuest.Contracts;

namespace Capstone.UI.Views;

public partial class MainWindow : Window
{
    private DevConsoleViewModel? _consoleVm;

    public MainWindow()
    {
        InitializeComponent();

        // Wire the developer console once the DataContext is available.
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is CatalogueViewModel vm)
        {
            // We need the backing IItemStore — reach it through the VM's store field via reflection
            // so we don't have to change the Core API.
            var storeField = typeof(CatalogueViewModel)
                .GetField("_store", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var store = storeField?.GetValue(vm) as IItemStore;

            if (store is not null)
            {
                _consoleVm = new DevConsoleViewModel(store);
                DevConsole.DataContext = _consoleVm;

                // Bind console visibility to the control.
                _consoleVm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(DevConsoleViewModel.IsVisible))
                    {
                        DevConsole.IsVisible = _consoleVm.IsVisible;
                        if (_consoleVm.IsVisible)
                            DevConsole.FocusInput();
                    }
                };

                DevConsole.IsVisible = false;
            }
        }
    }

    private CatalogueViewModel? Vm => DataContext as CatalogueViewModel;

    // ── Keyboard shortcut: F12 or ~ toggles console ───────────────────────────

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        bool isToggle = e.Key == Key.F12 ||
                        (e.Key == Key.OemTilde) ||
                        (e.Key == Key.Oem3);   // ` / ~ on most layouts

        if (isToggle)
        {
            _consoleVm?.Toggle();
            e.Handled = true;
        }
    }

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
