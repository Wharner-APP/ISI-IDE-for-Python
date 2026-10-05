using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISI.IDE.Models;
using ISI.IDE.Services;

namespace ISI.IDE.ViewModels;

/// <summary>GUI for pip: list, install and uninstall packages of the selected interpreter.</summary>
public sealed partial class PackagesViewModel : ObservableObject
{
    private readonly string _python;

    public PackagesViewModel(string pythonPath) => _python = pythonPath;

    public string PythonPath => _python;
    public ObservableCollection<PackageItem> Packages { get; } = new();

    [ObservableProperty] private PackageItem? _selected;
    [ObservableProperty] private string _installName = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _isBusy;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        Status = "Loading packages...";
        var (items, error) = await PipService.ListAsync(_python);
        Packages.Clear();
        foreach (var p in items) Packages.Add(p);
        Status = error.Length > 0 ? "Error: " + error : $"{items.Count} packages installed";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task InstallAsync()
    {
        var name = InstallName.Trim();
        if (name.Length == 0) return;
        IsBusy = true;
        Status = $"Installing {name}...";
        var r = await PipService.InstallAsync(_python, name);
        Status = r.ExitCode == 0 ? $"Installed {name}" : "Error: " + LastLine(r.StdErr);
        InstallName = "";
        IsBusy = false;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task UninstallAsync()
    {
        if (Selected is null) return;
        var name = Selected.Name;
        IsBusy = true;
        Status = $"Removing {name}...";
        var r = await PipService.UninstallAsync(_python, name);
        Status = r.ExitCode == 0 ? $"Removed {name}" : "Error: " + LastLine(r.StdErr);
        IsBusy = false;
        await RefreshAsync();
    }

    private static string LastLine(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? "";
}
