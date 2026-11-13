using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;
using SharpQuest.Contracts;

namespace Capstone.UI.DevConsole;

/// <summary>
/// View-model for the developer console.
/// Manages log output, command history, and the command interpreter.
/// Thread-safe logging via Dispatcher.UIThread.Post.
/// </summary>
public sealed class DevConsoleViewModel : INotifyPropertyChanged, IDisposable
{
    // ── State ────────────────────────────────────────────────────────────────

    private readonly IItemStore _store;
    private readonly TraceListenerBridge _traceBridge;

    public ObservableCollection<ConsoleLine> Lines { get; } = new();

    private readonly List<string> _history = new();
    private int _historyIndex = -1;

    private string _input = string.Empty;
    public string Input
    {
        get => _input;
        set { _input = value; Notify(nameof(Input)); }
    }

    private bool _isVisible;
    public bool IsVisible
    {
        get => _isVisible;
        set { _isVisible = value; Notify(nameof(IsVisible)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

    // ── Constructor ──────────────────────────────────────────────────────────

    public DevConsoleViewModel(IItemStore store)
    {
        _store = store;

        // Hook into Trace so any Trace.WriteLine calls appear in the console.
        _traceBridge = new TraceListenerBridge(this);
        Trace.Listeners.Add(_traceBridge);

        AppendLine("[DevConsole] Ready — press F12 or ~ to toggle. Type 'help' for commands.", LineKind.System);
    }

    // ── Toggle ───────────────────────────────────────────────────────────────

    public void Toggle() => IsVisible = !IsVisible;

    // ── History navigation ───────────────────────────────────────────────────

    public void HistoryUp()
    {
        if (_history.Count == 0) return;
        _historyIndex = Math.Clamp(_historyIndex + 1, 0, _history.Count - 1);
        Input = _history[_history.Count - 1 - _historyIndex];
    }

    public void HistoryDown()
    {
        if (_historyIndex <= 0) { _historyIndex = -1; Input = string.Empty; return; }
        _historyIndex--;
        Input = _history[_history.Count - 1 - _historyIndex];
    }

    // ── Command execution ────────────────────────────────────────────────────

    public void Execute()
    {
        var raw = Input.Trim();
        Input = string.Empty;
        _historyIndex = -1;

        if (string.IsNullOrEmpty(raw)) return;

        _history.Add(raw);
        AppendLine($"> {raw}", LineKind.Input);

        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd   = parts[0].ToLowerInvariant();
        var args  = parts.Skip(1).ToArray();

        try
        {
            switch (cmd)
            {
                case "help":    CmdHelp();             break;
                case "list":    CmdList();             break;
                case "count":   CmdCount();            break;
                case "find":    CmdFind(args);         break;
                case "remove":  CmdRemove(args);       break;
                case "clear":   CmdClear();            break;
                case "store":   CmdStore();            break;
                case "add":     CmdAddSample(args);    break;
                case "echo":    CmdEcho(args);         break;
                default:
                    AppendLine($"Unknown command '{cmd}'. Type 'help'.", LineKind.Error);
                    break;
            }
        }
        catch (Exception ex)
        {
            AppendLine($"[ERROR] {ex.Message}", LineKind.Error);
        }
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    private void CmdHelp()
    {
        var help = """
            Available commands:
              help                  — Show this message
              list                  — List all items in the store
              count                 — Show total item count
              find <text>           — Find items whose name contains <text>
              remove <id>           — Remove item by id
              add <title> [<year>]  — Add a quick digital game entry
              store                 — Dump raw store internals
              echo <text>           — Echo text back to the console
              clear                 — Clear the console output
            """;
        foreach (var line in help.Split('\n'))
            AppendLine(line, LineKind.System);
    }

    private void CmdList()
    {
        var all = _store.All();
        if (all.Count == 0) { AppendLine("Store is empty.", LineKind.Output); return; }
        foreach (var item in all)
            AppendLine($"  [{item.Id}]  {item.DisplayName}", LineKind.Output);
        AppendLine($"  — {all.Count} item(s) total", LineKind.System);
    }

    private void CmdCount()
        => AppendLine($"Store contains {_store.All().Count} item(s).", LineKind.Output);

    private void CmdFind(string[] args)
    {
        if (args.Length == 0) { AppendLine("Usage: find <text>", LineKind.Error); return; }
        var query = string.Join(' ', args);
        var results = _store.All().Where(i =>
            i.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        if (results.Count == 0) { AppendLine($"No items matching '{query}'.", LineKind.Output); return; }
        foreach (var item in results)
            AppendLine($"  [{item.Id}]  {item.DisplayName}", LineKind.Output);
    }

    private void CmdRemove(string[] args)
    {
        if (args.Length == 0) { AppendLine("Usage: remove <id>", LineKind.Error); return; }
        var id = args[0];
        var item = _store.FindById(id);
        if (item is null) { AppendLine($"No item with id '{id}'.", LineKind.Error); return; }
        _store.Remove(id);
        AppendLine($"Removed [{id}] {item.DisplayName}.", LineKind.Output);
    }

    private void CmdAddSample(string[] args)
    {
        if (args.Length == 0) { AppendLine("Usage: add <title> [<year>]", LineKind.Error); return; }
        var title = string.Join(' ', args.TakeWhile(a => !int.TryParse(a, out _)));
        if (string.IsNullOrWhiteSpace(title)) title = args[0];
        int? year = args.Select(a => int.TryParse(a, out var y) ? (int?)y : null).FirstOrDefault(y => y.HasValue);

        var id = $"dev-{title.ToLowerInvariant().Replace(' ', '-')}-{DateTime.UtcNow.Ticks % 10000}";
        var game = new Capstone.Core.Models.DigitalGame
        {
            Id          = id,
            Title       = title,
            Platform    = Platform.PC,
            Status      = GameStatus.Backlog,
            ReleaseYear = year,
            Developer   = new Capstone.Core.Models.Developer { Name = "Dev Console" }
        };

        _store.Add(game);
        AppendLine($"Added [{id}] '{title}'{(year.HasValue ? $" ({year})" : "")} to the store.", LineKind.Output);
    }

    private void CmdStore()
    {
        var all = _store.All();
        AppendLine($"IItemStore  Count={all.Count}  Type={_store.GetType().Name}", LineKind.System);
        foreach (var item in all)
            AppendLine($"  {item.GetType().Name,-20} Id={item.Id,-24} Name={item.DisplayName}", LineKind.Output);
    }

    private void CmdEcho(string[] args)
        => AppendLine(string.Join(' ', args), LineKind.Output);

    private void CmdClear() => Lines.Clear();

    // ── Logging ──────────────────────────────────────────────────────────────

    internal void AppendLine(string text, LineKind kind = LineKind.Output)
    {
        // Always marshal to the UI thread.
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var ts = DateTime.Now.ToString("HH:mm:ss.fff");
            Lines.Add(new ConsoleLine($"[{ts}] {text}", kind));

            // Trim to keep memory bounded.
            while (Lines.Count > 2000)
                Lines.RemoveAt(0);
        });
    }

    // ── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose()
    {
        Trace.Listeners.Remove(_traceBridge);
        _traceBridge.Dispose();
    }
}

// ── Supporting types ─────────────────────────────────────────────────────────

public enum LineKind { Input, Output, System, Error }

public sealed record ConsoleLine(string Text, LineKind Kind);

/// <summary>
/// Bridges System.Diagnostics.Trace output into the DevConsole.
/// </summary>
internal sealed class TraceListenerBridge : TraceListener
{
    private readonly DevConsoleViewModel _vm;
    private readonly StringBuilder _buf = new();

    public TraceListenerBridge(DevConsoleViewModel vm) => _vm = vm;

    public override void Write(string? message)
    {
        if (message is not null) _buf.Append(message);
    }

    public override void WriteLine(string? message)
    {
        _buf.Append(message);
        _vm.AppendLine(_buf.ToString(), LineKind.System);
        _buf.Clear();
    }
}

