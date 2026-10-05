namespace ISI.IDE.Services;

/// <summary>
/// Finds the Python interpreter.
/// Default: "&lt;app folder&gt;/Python 3.14/bin/python.exe" (python3 on Linux/macOS),
/// i.e. C:\Program Files\Wharner APP\ISI IDE for Python\Python 3.14\bin\python.exe.
/// The user can override it in Settings.
/// </summary>
public static class PythonLocator
{
    public const string BundledFolderName = "Python 3.14";

    public static string AppRoot => AppContext.BaseDirectory;

    /// <summary>The path shown as "default" in Settings.</summary>
    public static string DefaultPythonPath => Path.Combine(
        AppRoot, BundledFolderName, "bin", OperatingSystem.IsWindows() ? "python.exe" : "python3");

    /// <summary>Settings path if it exists, otherwise the bundled one, otherwise PATH.</summary>
    public static string? Resolve(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.PythonPath) && File.Exists(settings.PythonPath))
            return settings.PythonPath;

        foreach (var candidate in BundledCandidates())
            if (File.Exists(candidate)) return candidate;

        return FindOnPath();
    }

    public static bool IsDefault(AppSettings settings) =>
        string.IsNullOrWhiteSpace(settings.PythonPath);

    private static IEnumerable<string> BundledCandidates()
    {
        var root = Path.Combine(AppRoot, BundledFolderName);
        yield return DefaultPythonPath;
        if (OperatingSystem.IsWindows())
        {
            yield return Path.Combine(root, "python.exe");          // embeddable layout
            yield return Path.Combine(root, "bin", "python3.exe");
        }
        else
        {
            yield return Path.Combine(root, "bin", "python");
            yield return Path.Combine(root, "bin", "python3.14");
        }
    }

    private static string? FindOnPath()
    {
        var names = OperatingSystem.IsWindows()
            ? new[] { "python.exe" }
            : new[] { "python3", "python" };
        var dirs = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var dir in dirs)
            foreach (var name in names)
            {
                try
                {
                    var full = Path.Combine(dir.Trim('"'), name);
                    // Skip the Microsoft Store "App execution alias" stub on Windows.
                    if (File.Exists(full) && !full.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase))
                        return full;
                }
                catch (ArgumentException) { /* invalid PATH entry */ }
            }
        return null;
    }

    public static async Task<string> GetVersionAsync(string pythonPath)
    {
        var r = await ProcessUtil.RunAsync(pythonPath, new[] { "--version" });
        var text = (r.StdOut + r.StdErr).Trim();
        return r.ExitCode == 0 && text.Length > 0 ? text : "Not a working Python interpreter";
    }

    public static string BridgeScript(string name) =>
        Path.Combine(AppRoot, "PythonBridge", "isi_tools", name);
}
