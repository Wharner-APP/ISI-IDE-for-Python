using System.Text.Json;

namespace ISI.IDE.Services;

/// <summary>User settings, stored per user (not in the install folder, which may be read-only).</summary>
public sealed class AppSettings
{
    /// <summary>Interpreter chosen by the user. Null/empty = use the bundled default.</summary>
    public string? PythonPath { get; set; }
    public string Theme { get; set; } = "Dark";   // Dark | Light | System
    public double FontSize { get; set; } = 14;
    public bool WordWrap { get; set; }
    public bool ShowLineNumbers { get; set; } = true;
    public string? LastFolder { get; set; }

    public static AppSettings Current { get; private set; } = new();

    /// <summary>Raised on the calling thread after <see cref="Save"/>.</summary>
    public static event Action? Changed;

    public static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Wharner APP", "ISI IDE for Python");

    private static string FilePath => Path.Combine(SettingsDirectory, "settings.json");

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
                Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch
        {
            Current = new AppSettings(); // corrupted file: fall back to defaults
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(FilePath,
                JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Settings are best-effort; never crash the IDE because of a read-only profile.
        }
        Changed?.Invoke();
    }
}
