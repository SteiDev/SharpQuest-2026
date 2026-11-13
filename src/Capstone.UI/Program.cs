using Avalonia;
using Capstone.UI;

// Load .env so IGDB credentials are available.
LoadDotEnv(Path.Combine(AppContext.BaseDirectory, ".env"));
LoadDotEnv(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>()
              .UsePlatformDetect()
              .WithInterFont()
              .LogToTrace();

static void LoadDotEnv(string path)
{
    if (!File.Exists(path)) return;
    foreach (var raw in File.ReadAllLines(path))
    {
        var line = raw.Trim();
        if (line.Length == 0 || line.StartsWith('#')) continue;
        var eq = line.IndexOf('=');
        if (eq <= 0) continue;
        var key   = line[..eq].Trim();
        var value = line[(eq + 1)..].Trim().Trim('"').Trim('\'');
        if (!string.IsNullOrEmpty(key) && Environment.GetEnvironmentVariable(key) is null)
            Environment.SetEnvironmentVariable(key, value);
    }
}

