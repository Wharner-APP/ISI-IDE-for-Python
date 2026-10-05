using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ISI.IDE.ViewModels;

/// <summary>Node of the project tree. Directories load their children lazily on first expand.</summary>
public sealed partial class FileNodeViewModel : ObservableObject
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", "__pycache__", ".venv", "venv", "env", "node_modules", ".idea", ".vs",
        ".mypy_cache", ".pytest_cache", ".ruff_cache",
    };

    private bool _loaded;

    public FileNodeViewModel(string path, bool isDirectory)
    {
        Path = path;
        IsDirectory = isDirectory;
        Name = System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar));
        if (Name.Length == 0) Name = path;
        if (isDirectory) Children.Add(new FileNodeViewModel("", false)); // placeholder => shows expander
    }

    public string Path { get; }
    public string Name { get; }
    public bool IsDirectory { get; }
    public ObservableCollection<FileNodeViewModel> Children { get; } = new();

    [ObservableProperty] private bool _isExpanded;

    partial void OnIsExpandedChanged(bool value)
    {
        if (value) LoadChildren();
    }

    public void LoadChildren()
    {
        if (!IsDirectory || _loaded) return;
        _loaded = true;
        Children.Clear();
        try
        {
            var options = new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = FileAttributes.System };
            foreach (var dir in Directory.EnumerateDirectories(Path, "*", options)
                         .Where(d => !IgnoredDirectories.Contains(System.IO.Path.GetFileName(d)))
                         .OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
                Children.Add(new FileNodeViewModel(dir, true));

            foreach (var file in Directory.EnumerateFiles(Path, "*", options)
                         .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                Children.Add(new FileNodeViewModel(file, false));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Unreadable folder: show it as empty.
        }
    }
}
