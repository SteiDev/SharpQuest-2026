using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Capstone.UI.Views;

public enum SteamMergeMode { Cancel, Replace, Merge }

public partial class SteamMergeConfirmDialog : Window
{
    // Parameterless ctor for Avalonia's design-time previewer
    public SteamMergeConfirmDialog() : this(0, 0) { }

    public SteamMergeConfirmDialog(int currentCount, int importCount)
    {
        InitializeComponent();

        BodyText.Text =
            $"Your library has {currentCount} game{(currentCount == 1 ? "" : "s")}. " +
            $"You are about to import {importCount} game{(importCount == 1 ? "" : "s")} from Steam.";
    }

    private void OnMerge(object? sender, RoutedEventArgs e)   => Close(SteamMergeMode.Merge);
    private void OnReplace(object? sender, RoutedEventArgs e) => Close(SteamMergeMode.Replace);
    private void OnCancel(object? sender, RoutedEventArgs e)  => Close(SteamMergeMode.Cancel);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close(SteamMergeMode.Cancel);
        }
    }
}

