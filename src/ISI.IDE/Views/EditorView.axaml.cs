using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Folding;
using ISI.IDE.Editor;
using ISI.IDE.Models;
using ISI.IDE.Services;
using ISI.IDE.ViewModels;

namespace ISI.IDE.Views;

public partial class EditorView : UserControl
{
    private EditorTabViewModel? _vm;
    private PythonColorizer? _colorizer;
    private FoldingManager? _folding;
    private CompletionWindow? _completion;

    public EditorView()
    {
        InitializeComponent();
        Editor.Options.ConvertTabsToSpaces = true;
        Editor.Options.IndentationSize = 4;
        Editor.Options.HighlightCurrentLine = true;
        Editor.Options.EnableHyperlinks = false;
        Editor.Options.EnableEmailHyperlinks = false;
        Editor.TextArea.IndentationStrategy = new PythonIndentationStrategy();
        Editor.TextArea.TextEntered += OnTextEntered;
        Editor.TextArea.TextEntering += OnTextEntering;
        Editor.TextArea.Caret.PositionChanged += OnCaretChanged;
        ActualThemeVariantChanged += (_, _) => Editor.TextArea.TextView.Redraw();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AppSettings.Changed += ApplySettings;
        ApplySettings();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        AppSettings.Changed -= ApplySettings;
        Detach();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        Attach(DataContext as EditorTabViewModel);
    }

    private void ApplySettings()
    {
        var s = AppSettings.Current;
        Editor.FontSize = s.FontSize;
        Editor.WordWrap = s.WordWrap;
        Editor.ShowLineNumbers = s.ShowLineNumbers;
    }

    private void Attach(EditorTabViewModel? vm)
    {
        Detach();
        _vm = vm;
        if (vm is null) return;

        // Remember the caret: assigning Document resets it and would overwrite the VM values.
        int savedLine = vm.CaretLine, savedColumn = vm.CaretColumn;

        Editor.Document = vm.Document;
        _colorizer = new PythonColorizer(vm.Document);
        Editor.TextArea.TextView.LineTransformers.Add(_colorizer);
        _folding = FoldingManager.Install(Editor.TextArea);

        vm.AnalysisUpdated += OnAnalysisUpdated;
        vm.GoToRequested += GoTo;
        UpdateFoldings(vm.Analysis);

        var line = Math.Clamp(savedLine, 1, Editor.Document.LineCount);
        Editor.TextArea.Caret.Line = line;
        Editor.TextArea.Caret.Column = Math.Max(1, savedColumn);
        Editor.ScrollToLine(line);
        Editor.Focus();
    }

    private void Detach()
    {
        if (_vm is not null)
        {
            _vm.AnalysisUpdated -= OnAnalysisUpdated;
            _vm.GoToRequested -= GoTo;
        }
        if (_colorizer is not null)
        {
            Editor.TextArea.TextView.LineTransformers.Remove(_colorizer);
            _colorizer = null;
        }
        if (_folding is not null)
        {
            FoldingManager.Uninstall(_folding);
            _folding = null;
        }
        _vm = null;
    }

    private void OnAnalysisUpdated(EditorTabViewModel vm)
    {
        UpdateFoldings(vm.Analysis);
        Editor.TextArea.TextView.Redraw();   // multi-line strings may have changed
    }

    private void OnCaretChanged(object? sender, EventArgs e)
    {
        if (_vm is null) return;
        _vm.CaretLine = Editor.TextArea.Caret.Line;
        _vm.CaretColumn = Editor.TextArea.Caret.Column;
    }

    private void GoTo(int line)
    {
        line = Math.Clamp(line, 1, Editor.Document.LineCount);
        Editor.TextArea.Caret.Line = line;
        Editor.TextArea.Caret.Column = 1;
        Editor.ScrollToLine(line);
        Editor.Focus();
    }

    /// <summary>Indentation-based folding of every def/class found by the C++ analyzer.</summary>
    private void UpdateFoldings(AnalysisResult analysis)
    {
        if (_folding is null || _vm is null) return;
        var doc = _vm.Document;
        var lineCount = doc.LineCount;
        var foldings = new List<NewFolding>();

        foreach (var symbol in analysis.Symbols)
        {
            if (symbol.Line < 1 || symbol.Line >= lineCount) continue;
            var last = symbol.Line;
            for (var ln = symbol.Line + 1; ln <= lineCount; ln++)
            {
                var dl = doc.GetLineByNumber(ln);
                var text = doc.GetText(dl.Offset, dl.Length);
                if (string.IsNullOrWhiteSpace(text)) continue;
                var indent = 0;
                while (indent < text.Length && (text[indent] == ' ' || text[indent] == '\t')) indent++;
                if (indent <= symbol.Indent) break;
                last = ln;
            }
            if (last > symbol.Line)
            {
                var head = doc.GetLineByNumber(symbol.Line);
                var tail = doc.GetLineByNumber(last);
                foldings.Add(new NewFolding(head.EndOffset, tail.EndOffset) { Name = "..." });
            }
        }
        foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        _folding.UpdateFoldings(foldings, -1);
    }

    // ---- code completion ------------------------------------------------------
    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (_completion is not null && !string.IsNullOrEmpty(e.Text) && !IsIdentifierChar(e.Text[0]))
            _completion.CompletionList.RequestInsertion(e);
    }

    private void OnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (_completion is not null || string.IsNullOrEmpty(e.Text)) return;
        var c = e.Text[0];
        if (!IsIdentifierChar(c) && c != '.') return;

        var doc = Editor.Document;
        var caret = Editor.CaretOffset;
        var start = caret;
        while (start > 0 && IsIdentifierChar(doc.GetCharAt(start - 1))) start--;
        var prefix = doc.GetText(start, caret - start);
        var afterDot = c == '.' || (start > 0 && doc.GetCharAt(start - 1) == '.');
        if (c != '.' && prefix.Length < 2) return;

        var items = CompletionProvider.Get(doc.Text, c == '.' ? "" : prefix, afterDot);
        if (items.Count == 0) return;

        _completion = new CompletionWindow(Editor.TextArea) { StartOffset = c == '.' ? caret : start };
        foreach (var item in items) _completion.CompletionList.CompletionData.Add(item);
        _completion.Closed += (_, _) => _completion = null;
        _completion.Show();
    }
}
