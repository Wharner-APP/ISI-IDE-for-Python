using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ISI.IDE.ViewModels;

namespace ISI.IDE.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm = new();

    public SettingsWindow()
    {
        InitializeComponent();
        Icon = Branding.CreateIcon();
        DataContext = _vm;
        Opened += async (_, _) => await _vm.DetectAsync();
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Python interpreter",
            AllowMultiple = false,
        });
        if (files.Count == 0) return;
        _vm.PythonPath = files[0].Path.LocalPath;
        await _vm.DetectAsync();
    }

    private async void OnResetClick(object? sender, RoutedEventArgs e)
    {
        _vm.ResetPython();
        await _vm.DetectAsync();
    }

    private async void OnCheckClick(object? sender, RoutedEventArgs e) => await _vm.DetectAsync();

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        _vm.Apply();
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();
}
