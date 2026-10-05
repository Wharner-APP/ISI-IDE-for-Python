using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Document;
using ISI.IDE.Models;
using ISI.IDE.ViewModels;

namespace ISI.IDE.Views;

public partial class MainWindow : Window, IDialogService
{
    private bool _closeApproved;

    public MainWindow()
    {
        InitializeComponent();
        Icon = Branding.CreateIcon();
        ConsoleEditor.Document = new TextDocument();
        DataContextChanged += OnDataContextChanged;
        Closing += OnClosing;
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (ViewModel is not { } vm) return;
        vm.ConsoleWrite += AppendConsole;
        vm.ConsoleClear += () => ConsoleEditor.Document.Text = "";
    }

    private void AppendConsole(string text)
    {
        var doc = ConsoleEditor.Document;
        if (doc.TextLength > 400_000) doc.Remove(0, 100_000);   // keep memory bounded
        doc.Insert(doc.TextLength, text);
        ConsoleEditor.ScrollToLine(doc.LineCount);
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeApproved || ViewModel is not { } vm) return;
        e.Cancel = true;                       // decide asynchronously (unsaved changes dialog)
        if (await vm.ConfirmExitAsync())
        {
            _closeApproved = true;
            Close();
        }
    }

    // ---- control event handlers ------------------------------------------------
    private void OnProjectDoubleTapped(object? sender, TappedEventArgs e) =>
        ViewModel?.OpenNode(ViewModel.SelectedNode);

    private void OnSymbolDoubleTapped(object? sender, TappedEventArgs e) =>
        ViewModel?.GoToSymbol(SymbolList.SelectedItem as SymbolItem);

    private void OnProblemDoubleTapped(object? sender, TappedEventArgs e) =>
        ViewModel?.GoToProblem(ProblemList.SelectedItem as DiagnosticItem);

    // ---- IDialogService ----------------------------------------------------------
    public async Task<string?> PickFolderAsync(string title)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = title, AllowMultiple = false });
        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }

    public async Task<string?> PickFileAsync(string title)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = title, AllowMultiple = false });
        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    public async Task<string?> SaveFileAsync(string title, string suggestedName)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = "py",
        });
        return file?.Path.LocalPath;
    }

    public async Task<string?> PromptAsync(string title, string message, string initial = "")
    {
        var r = await MessageDialog.ShowAsync(this, title, message, new[] { "OK", "Cancel" }, true, initial);
        return r is { ButtonIndex: 0 } ? r.Text : null;
    }

    public async Task<SaveChoice> AskSaveAsync(string fileName)
    {
        var r = await MessageDialog.ShowAsync(this, "Unsaved changes",
            $"Save changes to \"{fileName}\" before closing?", new[] { "Save", "Don't save", "Cancel" });
        return r?.ButtonIndex switch
        {
            0 => SaveChoice.Save,
            1 => SaveChoice.Discard,
            _ => SaveChoice.Cancel,
        };
    }

    public Task ShowSettingsAsync() => new SettingsWindow().ShowDialog(this);
    public Task ShowAboutAsync() => new AboutWindow().ShowDialog(this);
    public Task ShowPackagesAsync(string pythonPath) => new PackagesWindow(pythonPath).ShowDialog(this);

    public void CloseMainWindow() => Close();
}
