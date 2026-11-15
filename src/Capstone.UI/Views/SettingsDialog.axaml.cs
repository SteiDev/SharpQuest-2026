using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Capstone.UI.Views;

public partial class SettingsDialog : Window
{
    private bool _inEditorView = false;
    private string _activeEnvPath = string.Empty;

    public SettingsDialog()
    {
        InitializeComponent();
        _activeEnvPath = ResolveDotEnvPath();
        EnvFilePathLabel.Text = _activeEnvPath;
    }

    // ── Navigation between views ──────────────────────────────────────────────

    private void OnOpenEnvEditorClick(object? sender, RoutedEventArgs e)
    {
        _inEditorView = true;
        MenuView.IsVisible = false;
        EnvEditorView.IsVisible = true;

        HeaderTitle.Text = "Environment Variables (.env)";
        HeaderSubtitle.Text = "Edit application credentials and runtime configuration";
        SaveStatusText.Text = string.Empty;

        // Load existing .env content
        EnvContentBox.Text = ReadEnvFile();
        EnvFilePathLabel.Text = _activeEnvPath;
    }

    private void OnBackToMenuClick(object? sender, RoutedEventArgs e)
    {
        SwitchToMenu();
    }

    private void SwitchToMenu()
    {
        _inEditorView = false;
        EnvEditorView.IsVisible = false;
        MenuView.IsVisible = true;

        HeaderTitle.Text = "Settings";
        HeaderSubtitle.Text = "Configure SharpQuest preferences and integrations";
        SaveStatusText.Text = string.Empty;
    }

    // ── Keydown handling: ESC goes back or closes ─────────────────────────────

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (_inEditorView)
            {
                SwitchToMenu();
                e.Handled = true;
            }
            else
            {
                Close();
                e.Handled = true;
            }
        }
    }

    // ── Template loading ──────────────────────────────────────────────────────

    private void OnLoadTemplateClick(object? sender, RoutedEventArgs e)
    {
        var examplePath = ResolveExamplePath();
        if (File.Exists(examplePath))
        {
            EnvContentBox.Text = File.ReadAllText(examplePath);
            SaveStatusText.Text = "Loaded .env.example template into editor.";
        }
        else
        {
            EnvContentBox.Text =
                "# IGDB API credentials\n" +
                "IGDB_CLIENT_ID=your_client_id_here\n" +
                "IGDB_CLIENT_SECRET=your_client_secret_here\n";
            SaveStatusText.Text = "Loaded default template into editor.";
        }
    }

    // ── Save .env and apply to current process ────────────────────────────────

    private void OnSaveEnvClick(object? sender, RoutedEventArgs e)
    {
        var content = EnvContentBox.Text ?? string.Empty;

        try
        {
            // 1. Write to primary .env file
            var dir = Path.GetDirectoryName(_activeEnvPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(_activeEnvPath, content);

            // 2. Also sync to AppContext.BaseDirectory/.env if different
            var baseDirEnv = Path.Combine(AppContext.BaseDirectory, ".env");
            if (!string.Equals(Path.GetFullPath(_activeEnvPath), Path.GetFullPath(baseDirEnv), StringComparison.OrdinalIgnoreCase))
            {
                try { File.WriteAllText(baseDirEnv, content); } catch { /* ignore */ }
            }

            // 3. Also sync to Directory.GetCurrentDirectory()/.env if different
            var cwdEnv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (!string.Equals(Path.GetFullPath(_activeEnvPath), Path.GetFullPath(cwdEnv), StringComparison.OrdinalIgnoreCase))
            {
                try { File.WriteAllText(cwdEnv, content); } catch { /* ignore */ }
            }

            // 4. Apply KEY=VALUE variables to live active process
            int count = ApplyEnvVariables(content);

            SaveStatusText.Text = $"✓ Saved to .env and applied {count} variable(s) to active session.";
        }
        catch (Exception ex)
        {
            SaveStatusText.Text = $"Error saving: {ex.Message}";
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    // ── Helper methods ────────────────────────────────────────────────────────

    private string ReadEnvFile()
    {
        if (File.Exists(_activeEnvPath))
            return File.ReadAllText(_activeEnvPath);

        // Fallback to .env.example if .env doesn't exist yet
        var examplePath = ResolveExamplePath();
        if (File.Exists(examplePath))
            return File.ReadAllText(examplePath);

        return "# Environment Variables\nIGDB_CLIENT_ID=\nIGDB_CLIENT_SECRET=\n";
    }

    private static int ApplyEnvVariables(string content)
    {
        int count = 0;
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            int eq = line.IndexOf('=');
            if (eq <= 0) continue;

            string key = line[..eq].Trim();
            string value = line[(eq + 1)..].Trim().Trim('"').Trim('\'');

            if (!string.IsNullOrEmpty(key))
            {
                Environment.SetEnvironmentVariable(key, value);
                count++;
            }
        }
        return count;
    }

    private static string ResolveDotEnvPath()
    {
        // Check current working directory
        var cwd = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(cwd)) return cwd;

        // Traverse upwards from current directory to find repo root or .env
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, ".env");
            if (File.Exists(candidate)) return candidate;
            var git = Path.Combine(current.FullName, ".git");
            if (Directory.Exists(git)) return candidate;
            current = current.Parent;
        }

        // Traverse upwards from BaseDirectory
        current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, ".env");
            if (File.Exists(candidate)) return candidate;
            var git = Path.Combine(current.FullName, ".git");
            if (Directory.Exists(git)) return candidate;
            current = current.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, ".env");
    }

    private static string ResolveExamplePath()
    {
        var envPath = ResolveDotEnvPath();
        var dir = Path.GetDirectoryName(envPath);
        if (!string.IsNullOrEmpty(dir))
        {
            var ex = Path.Combine(dir, ".env.example");
            if (File.Exists(ex)) return ex;
        }

        var cwdEx = Path.Combine(Directory.GetCurrentDirectory(), ".env.example");
        if (File.Exists(cwdEx)) return cwdEx;

        return Path.Combine(AppContext.BaseDirectory, ".env.example");
    }
}

