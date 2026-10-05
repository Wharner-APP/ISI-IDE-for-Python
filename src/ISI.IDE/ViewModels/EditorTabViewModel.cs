using System.Text;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISI.IDE.Models;
using ISI.IDE.Services;

namespace ISI.IDE.ViewModels;

/// <summary>One open document: text, path, dirty flag and the latest code analysis.</summary>
public sealed partial class EditorTabViewModel : ObservableObject
{
    private CancellationTokenSource? _analysisCts;

    public EditorTabViewModel(string text, string? filePath)
    {
        Document = new TextDocument(text);
        _filePath = filePath;
        Document.TextChanged += (_, _) =>
        {
            IsDirty = true;
            ScheduleAnalysis();
        };
        ScheduleAnalysis();
    }

    public TextDocument Document { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(Header))]
    private string? _filePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Header))]
    private bool _isDirty;

    [ObservableProperty] private int _caretLine = 1;
    [ObservableProperty] private int _caretColumn = 1;

    public string Title => FilePath is null ? "untitled.py" : Path.GetFileName(FilePath);
    public string Header => IsDirty ? Title + " *" : Title;

    public AnalysisResult Analysis { get; private set; } = AnalysisResult.Empty;

    public event Action<EditorTabViewModel>? AnalysisUpdated;
    public event Action<EditorTabViewModel>? CloseRequested;
    public event Action<int>? GoToRequested;

    public static EditorTabViewModel Open(string path) =>
        new(File.ReadAllText(path), path);   // detects UTF-8/UTF-16 BOMs, defaults to UTF-8

    public void Save() => SaveAs(FilePath ?? throw new InvalidOperationException("No file path."));

    public void SaveAs(string path)
    {
        File.WriteAllText(path, Document.Text, new UTF8Encoding(false));
        FilePath = path;
        IsDirty = false;
    }

    public void RequestGoTo(int line) => GoToRequested?.Invoke(line);

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this);

    /// <summary>Debounced analysis on a worker thread (keeps typing latency low).</summary>
    private void ScheduleAnalysis()
    {
        _analysisCts?.Cancel();
        var cts = _analysisCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, cts.Token);
                var text = await Dispatcher.UIThread.InvokeAsync(() => Document.Text);
                if (cts.IsCancellationRequested) return;
                var result = CodeIntelligence.Analyze(text);
                if (cts.IsCancellationRequested) return;
                Dispatcher.UIThread.Post(() =>
                {
                    Analysis = result;
                    AnalysisUpdated?.Invoke(this);
                });
            }
            catch (OperationCanceledException) { }
        });
    }
}
