using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISI.IDE.Models;
using ISI.IDE.Services;

namespace ISI.IDE.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private const string WelcomeCode =
        "# Welcome to ISI IDE for Python!\n" +
        "# Press F5 (or the Run button) to run this file.\n\n" +
        "name = input(\"What is your name? \")\n" +
        "print(f\"Hello, {name}! Welcome to ISI IDE for Python.\")\n";

    private readonly IDialogService _dialogs;
    private readonly ScriptRunner _runner = new();
    private bool _exitConfirmed;

    public MainWindowViewModel(IDialogService dialogs)
    {
        _dialogs = dialogs;
        _runner.OutputReceived += text => Dispatcher.UIThread.Post(() => Write(text));
        _runner.Exited += code => Dispatcher.UIThread.Post(() =>
        {
            Write($"\nProcess finished with exit code {code}\n");
            IsRunning = false;
        });

        RefreshInterpreterText();
        var last = AppSettings.Current.LastFolder;
        if (last is not null && Directory.Exists(last)) SetProject(last);
        OpenUntitled(WelcomeCode);
        StatusText = "Ready";
    }

    // ---- bindable state -------------------------------------------------
    public ObservableCollection<FileNodeViewModel> ProjectRoots { get; } = new();
    public ObservableCollection<EditorTabViewModel> Tabs { get; } = new();
    public ObservableCollection<SymbolItem> Symbols { get; } = new();
    public ObservableCollection<DiagnosticItem> Problems { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private EditorTabViewModel? _selectedTab;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string? _projectPath;

    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _branchText = "";
    [ObservableProperty] private string _caretText = "";
    [ObservableProperty] private string _interpreterText = "";
    [ObservableProperty] private string _inputLine = "";
    [ObservableProperty] private int _bottomTabIndex;
    [ObservableProperty] private FileNodeViewModel? _selectedNode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isRunning;

    public string CoreText => CodeIntelligence.IsNativeAvailable
        ? $"C++ core {CodeIntelligence.NativeVersion}"
        : "C++ core: not loaded (managed fallback)";

    public string WindowTitle
    {
        get
        {
            var file = SelectedTab?.Title;
            var project = ProjectPath is null ? null : Path.GetFileName(ProjectPath.TrimEnd(Path.DirectorySeparatorChar));
            return string.Join(" - ", new[] { file, project, Branding.ProductName }.Where(s => !string.IsNullOrEmpty(s)));
        }
    }

    // ---- console bridge (the view appends text to an AvaloniaEdit editor) --
    public event Action<string>? ConsoleWrite;
    public event Action? ConsoleClear;
    private void Write(string text) => ConsoleWrite?.Invoke(text);

    // ---- selection / panels ---------------------------------------------
    partial void OnSelectedTabChanged(EditorTabViewModel? oldValue, EditorTabViewModel? newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= OnSelectedTabPropertyChanged;
        if (newValue is not null) newValue.PropertyChanged += OnSelectedTabPropertyChanged;
        UpdateCaretText();
        RefreshPanels(newValue);
    }

    private void OnSelectedTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorTabViewModel.CaretLine) or nameof(EditorTabViewModel.CaretColumn))
            UpdateCaretText();
        if (e.PropertyName is nameof(EditorTabViewModel.Title))
            OnPropertyChanged(nameof(WindowTitle));
    }

    private void UpdateCaretText() =>
        CaretText = SelectedTab is null ? "" : $"Ln {SelectedTab.CaretLine}, Col {SelectedTab.CaretColumn}";

    private void RefreshPanels(EditorTabViewModel? tab)
    {
        Symbols.Clear();
        Problems.Clear();
        if (tab is null) return;
        foreach (var s in tab.Analysis.Symbols) Symbols.Add(s);
        foreach (var d in tab.Analysis.Diagnostics) Problems.Add(d);
    }

    private void OnTabAnalysisUpdated(EditorTabViewModel tab)
    {
        if (tab == SelectedTab) RefreshPanels(tab);
    }

    // ---- files & project --------------------------------------------------
    private void SetProject(string path)
    {
        ProjectPath = path;
        ProjectRoots.Clear();
        var root = new FileNodeViewModel(path, true);
        root.IsExpanded = true;
        ProjectRoots.Add(root);
        AppSettings.Current.LastFolder = path;
        AppSettings.Save();
        _ = RefreshBranchAsync();
    }

    private EditorTabViewModel AddTab(EditorTabViewModel tab)
    {
        tab.CloseRequested += t => _ = CloseTabAsync(t);
        tab.AnalysisUpdated += OnTabAnalysisUpdated;
        Tabs.Add(tab);
        SelectedTab = tab;
        return tab;
    }

    [RelayCommand]
    private void NewFile() => OpenUntitled("");

    private void OpenUntitled(string initialText) => AddTab(new EditorTabViewModel(initialText, null));

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        var path = await _dialogs.PickFolderAsync("Open project folder");
        if (path is not null) SetProject(path);
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var path = await _dialogs.PickFileAsync("Open file");
        if (path is not null) OpenPath(path);
    }

    public void OpenPath(string path)
    {
        var existing = Tabs.FirstOrDefault(t => string.Equals(t.FilePath, path, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) { SelectedTab = existing; return; }
        try
        {
            var info = new FileInfo(path);
            if (info.Length > 20 * 1024 * 1024)
            {
                StatusText = "File is too large to open (> 20 MB)";
                return;
            }
            AddTab(EditorTabViewModel.Open(path));
            StatusText = "Opened " + path;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText = "Cannot open file: " + ex.Message;
        }
    }

    public void OpenNode(FileNodeViewModel? node)
    {
        if (node is { IsDirectory: false, Path.Length: > 0 }) OpenPath(node.Path);
    }

    [RelayCommand]
    private void RefreshProject()
    {
        if (ProjectPath is not null) SetProject(ProjectPath);
    }

    [RelayCommand]
    private async Task SaveAsync() { if (SelectedTab is not null) await SaveTabAsync(SelectedTab); }

    [RelayCommand]
    private async Task SaveAsAsync() { if (SelectedTab is not null) await SaveTabAsAsync(SelectedTab); }

    [RelayCommand]
    private async Task SaveAllAsync()
    {
        foreach (var t in Tabs.Where(t => t.IsDirty).ToList()) await SaveTabAsync(t);
    }

    private async Task<bool> SaveTabAsync(EditorTabViewModel tab)
    {
        if (tab.FilePath is null) return await SaveTabAsAsync(tab);
        try { tab.Save(); StatusText = "Saved " + tab.FilePath; return true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText = "Save failed: " + ex.Message;
            return false;
        }
    }

    private async Task<bool> SaveTabAsAsync(EditorTabViewModel tab)
    {
        var path = await _dialogs.SaveFileAsync("Save file", tab.Title);
        if (path is null) return false;
        try { tab.SaveAs(path); StatusText = "Saved " + path; return true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText = "Save failed: " + ex.Message;
            return false;
        }
    }

    private async Task CloseTabAsync(EditorTabViewModel tab)
    {
        if (tab.IsDirty)
        {
            var choice = await _dialogs.AskSaveAsync(tab.Title);
            if (choice == SaveChoice.Cancel) return;
            if (choice == SaveChoice.Save && !await SaveTabAsync(tab)) return;
        }
        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        if (SelectedTab == tab || SelectedTab is null)
            SelectedTab = Tabs.Count == 0 ? null : Tabs[Math.Min(index, Tabs.Count - 1)];
    }

    /// <summary>Called by the window on close. Returns true when the app may exit.</summary>
    public async Task<bool> ConfirmExitAsync()
    {
        if (_exitConfirmed) return true;
        foreach (var tab in Tabs.Where(t => t.IsDirty).ToList())
        {
            SelectedTab = tab;
            var choice = await _dialogs.AskSaveAsync(tab.Title);
            if (choice == SaveChoice.Cancel) return false;
            if (choice == SaveChoice.Save && !await SaveTabAsync(tab)) return false;
        }
        _runner.Stop();
        _exitConfirmed = true;
        return true;
    }

    [RelayCommand]
    private void Exit() => _dialogs.CloseMainWindow();

    // ---- run --------------------------------------------------------------
    [RelayCommand]
    private async Task RunAsync()
    {
        var tab = SelectedTab;
        if (tab is null) return;
        if (_runner.IsRunning) { Write("A process is already running. Press Stop first.\n"); return; }
        if (!await SaveTabAsync(tab) || tab.FilePath is null) return;

        var python = PythonLocator.Resolve(AppSettings.Current);
        if (python is null)
        {
            BottomTabIndex = 0;
            ConsoleClear?.Invoke();
            Write("Python interpreter not found.\n\n" +
                  $"Expected: {PythonLocator.DefaultPythonPath}\n" +
                  "Put Python into the 'Python 3.14' folder next to the IDE, or choose another interpreter\n" +
                  "in Tools > Settings.\n");
            return;
        }

        BottomTabIndex = 0;
        ConsoleClear?.Invoke();
        Write($"$ {python} -u \"{tab.FilePath}\"\n");
        var env = new Dictionary<string, string>();
        if (ProjectPath is not null) env["PYTHONPATH"] = ProjectPath;
        try
        {
            _runner.Start(python, new[] { "-u", tab.FilePath }, Path.GetDirectoryName(tab.FilePath)!, env);
            IsRunning = true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Write("Failed to start the interpreter: " + ex.Message + "\n");
        }
    }

    private bool CanStop() => IsRunning;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        _runner.Stop();
        Write("\n[stopped by user]\n");
    }

    [RelayCommand]
    private void SendInput()
    {
        if (!_runner.IsRunning) return;
        _runner.WriteInput(InputLine);
        Write(InputLine + "\n");   // pipes do not echo; show what was sent
        InputLine = "";
    }

    [RelayCommand]
    private void ClearConsole() => ConsoleClear?.Invoke();

    // ---- lint (Python bridge) ---------------------------------------------
    [RelayCommand]
    private async Task LintAsync()
    {
        var tab = SelectedTab;
        if (tab is null) return;
        if (!await SaveTabAsync(tab) || tab.FilePath is null) return;
        var python = PythonLocator.Resolve(AppSettings.Current);
        if (python is null) { StatusText = "Python interpreter not found"; return; }

        StatusText = "Checking...";
        var script = PythonLocator.BridgeScript("check.py");
        var r = await ProcessUtil.RunAsync(python, new[] { script, tab.FilePath });
        if (r.ExitCode != 0)
        {
            StatusText = "Check failed: " + r.StdErr.Trim();
            return;
        }
        try
        {
            using var doc = JsonDocument.Parse(r.StdOut);
            Problems.Clear();
            foreach (var d in tab.Analysis.Diagnostics) Problems.Add(d);   // structural findings from C++
            foreach (var e in doc.RootElement.EnumerateArray())
                Problems.Add(new DiagnosticItem(
                    e.GetProperty("severity").GetString() ?? "warning",
                    e.GetProperty("message").GetString() ?? "",
                    e.GetProperty("line").GetInt32(),
                    e.GetProperty("column").GetInt32()));
            BottomTabIndex = 1;
            StatusText = Problems.Count == 0 ? "No problems found" : $"{Problems.Count} problem(s)";
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException)
        {
            StatusText = "Unexpected checker output";
        }
    }

    public void GoToProblem(DiagnosticItem? item) => SelectedTab?.RequestGoTo(item?.Line ?? 1);
    public void GoToSymbol(SymbolItem? item) => SelectedTab?.RequestGoTo(item?.Line ?? 1);

    // ---- git --------------------------------------------------------------
    private async Task RunGitAsync(params string[] args)
    {
        if (ProjectPath is null) { StatusText = "Open a project folder first"; return; }
        BottomTabIndex = 0;
        Write($"$ git {string.Join(' ', args)}\n");
        var r = await GitService.RunAsync(ProjectPath, args);
        Write(r.StdOut);
        Write(r.StdErr);
        if (r.ExitCode != 0) Write($"[git exited with code {r.ExitCode}]\n");
        await RefreshBranchAsync();
    }

    private async Task RefreshBranchAsync()
    {
        if (ProjectPath is null) { BranchText = ""; return; }
        var branch = await GitService.GetBranchAsync(ProjectPath);
        BranchText = branch.Length > 0 ? "git: " + branch : "";
    }

    [RelayCommand] private Task GitStatusAsync() => RunGitAsync("status", "--short", "--branch");
    [RelayCommand] private Task GitPullAsync() => RunGitAsync("pull");
    [RelayCommand] private Task GitPushAsync() => RunGitAsync("push");
    [RelayCommand] private Task GitLogAsync() => RunGitAsync("log", "--oneline", "--graph", "--decorate", "-n", "30");

    [RelayCommand]
    private async Task GitCommitAsync()
    {
        if (ProjectPath is null) { StatusText = "Open a project folder first"; return; }
        var message = await _dialogs.PromptAsync("Git commit", "Commit message (all changes will be staged):");
        if (string.IsNullOrWhiteSpace(message)) return;
        await RunGitAsync("add", "-A");
        await RunGitAsync("commit", "-m", message.Trim());
    }

    // ---- tools / dialogs ----------------------------------------------------
    private void RefreshInterpreterText()
    {
        var path = PythonLocator.Resolve(AppSettings.Current);
        InterpreterText = path is null
            ? "Python: not found"
            : "Python: " + path + (PythonLocator.IsDefault(AppSettings.Current) ? " (default)" : "");
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        await _dialogs.ShowSettingsAsync();
        RefreshInterpreterText();
    }

    [RelayCommand]
    private async Task OpenPackagesAsync()
    {
        var python = PythonLocator.Resolve(AppSettings.Current);
        if (python is null) { StatusText = "Python interpreter not found"; return; }
        await _dialogs.ShowPackagesAsync(python);
    }

    [RelayCommand] private Task OpenAboutAsync() => _dialogs.ShowAboutAsync();
    [RelayCommand] private void OpenWebsite() => Os.OpenUrl(Branding.Website);

    [RelayCommand]
    private void ToggleTheme()
    {
        AppSettings.Current.Theme = AppSettings.Current.Theme == "Dark" ? "Light" : "Dark";
        AppSettings.Save();
        App.ApplyTheme(AppSettings.Current.Theme);
    }
}
