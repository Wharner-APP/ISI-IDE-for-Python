using CommunityToolkit.Mvvm.ComponentModel;
using ISI.IDE.Services;

namespace ISI.IDE.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    public SettingsViewModel()
    {
        var s = AppSettings.Current;
        _pythonPath = string.IsNullOrWhiteSpace(s.PythonPath) ? PythonLocator.DefaultPythonPath : s.PythonPath!;
        _theme = s.Theme;
        _fontSize = s.FontSize;
        _wordWrap = s.WordWrap;
        _showLineNumbers = s.ShowLineNumbers;
    }

    public string[] Themes { get; } = { "Dark", "Light", "System" };
    public string DefaultPythonPath => PythonLocator.DefaultPythonPath;

    [ObservableProperty] private string _pythonPath;
    [ObservableProperty] private string _pythonInfo = "";
    [ObservableProperty] private string _theme;
    [ObservableProperty] private double _fontSize;
    [ObservableProperty] private bool _wordWrap;
    [ObservableProperty] private bool _showLineNumbers;

    public void ResetPython() => PythonPath = PythonLocator.DefaultPythonPath;

    public async Task DetectAsync()
    {
        PythonInfo = File.Exists(PythonPath)
            ? await PythonLocator.GetVersionAsync(PythonPath)
            : "File not found";
    }

    public void Apply()
    {
        var s = AppSettings.Current;
        var path = PythonPath.Trim();
        // Storing null keeps following the bundled default (survives moving the install folder).
        s.PythonPath = string.Equals(path, PythonLocator.DefaultPythonPath, StringComparison.OrdinalIgnoreCase)
                       || path.Length == 0 ? null : path;
        s.Theme = Theme;
        s.FontSize = Math.Clamp(FontSize, 8, 40);
        s.WordWrap = WordWrap;
        s.ShowLineNumbers = ShowLineNumbers;
        AppSettings.Save();
        App.ApplyTheme(s.Theme);
    }
}
