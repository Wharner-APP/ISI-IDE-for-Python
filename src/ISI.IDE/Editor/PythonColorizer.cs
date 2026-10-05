using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace ISI.IDE.Editor;

/// <summary>
/// Fully managed Python syntax highlighter (works on every platform/architecture,
/// no native TextMate dependency). Handles comments, strings (incl. triple-quoted,
/// multi-line), numbers, decorators, keywords, builtins, def/class names.
/// </summary>
public sealed class PythonColorizer : DocumentColorizingTransformer
{
    public static readonly HashSet<string> Keywords = new()
    {
        "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class",
        "continue", "def", "del", "elif", "else", "except", "finally", "for", "from", "global",
        "if", "import", "in", "is", "lambda", "nonlocal", "not", "or", "pass", "raise",
        "return", "try", "while", "with", "yield",
    };

    public static readonly HashSet<string> Builtins = new()
    {
        "abs", "all", "any", "bool", "bytes", "callable", "chr", "dict", "dir", "divmod",
        "enumerate", "filter", "float", "format", "frozenset", "getattr", "hasattr", "hash",
        "help", "id", "input", "int", "isinstance", "issubclass", "iter", "len", "list", "map",
        "max", "min", "next", "object", "open", "ord", "pow", "print", "property", "range",
        "repr", "reversed", "round", "set", "setattr", "slice", "sorted", "staticmethod",
        "str", "sum", "super", "tuple", "type", "vars", "zip",
        "Exception", "ValueError", "TypeError", "KeyError", "IndexError", "RuntimeError",
        "OSError", "AttributeError", "ImportError", "StopIteration", "NotImplementedError",
    };

    private sealed record Palette(
        IBrush Keyword, IBrush Builtin, IBrush String, IBrush Number,
        IBrush Comment, IBrush Decorator, IBrush Function, IBrush Class, IBrush SelfRef);

    private static IBrush Solid(string hex) => new ImmutableSolidColorBrush(Color.Parse(hex));

    private static readonly Palette Dark = new(
        Solid("#C586C0"), Solid("#4EC9B0"), Solid("#CE9178"), Solid("#B5CEA8"),
        Solid("#6A9955"), Solid("#DCDCAA"), Solid("#DCDCAA"), Solid("#4EC9B0"), Solid("#9CDCFE"));

    private static readonly Palette Light = new(
        Solid("#AF00DB"), Solid("#267F99"), Solid("#A31515"), Solid("#098658"),
        Solid("#008000"), Solid("#795E26"), Solid("#795E26"), Solid("#267F99"), Solid("#001080"));

    private static readonly Regex TokenRegex = new(
        @"(?<comment>#[^\r\n]*)" +
        @"|(?<string>(?:[rRbBuUfF]{1,2})?(?:'(?:\\.|[^'\\\r\n])*'?|""(?:\\.|[^""\\\r\n])*""?))" +
        @"|(?<number>\b0[xX][0-9a-fA-F_]+\b|\b\d[\d_]*(?:\.\d+)?(?:[eE][+-]?\d+)?j?\b)" +
        @"|(?<deco>@[A-Za-z_][\w.]*)" +
        @"|(?<ident>[A-Za-z_]\w*)",
        RegexOptions.Compiled);

    // Triple-quoted strings can span lines, so they are located on the whole document.
    private static readonly Regex TripleRegex = new(
        @"(?<q>""""""|''')[\s\S]*?(?:\k<q>|\z)", RegexOptions.Compiled);

    private readonly TextDocument _document;
    private List<(int Start, int End)>? _triples;

    public PythonColorizer(TextDocument document)
    {
        _document = document;
        _document.Changed += (_, _) => _triples = null;   // recomputed lazily
    }

    private static bool IsDark => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    private List<(int Start, int End)> GetTriples()
    {
        if (_triples is not null) return _triples;
        var list = new List<(int, int)>();
        foreach (Match m in TripleRegex.Matches(_document.Text))
            list.Add((m.Index, m.Index + m.Length));
        return _triples = list;
    }

    /// <summary>Index of the first region whose End is greater than <paramref name="pos"/>.</summary>
    private static int FirstRegionEndingAfter(List<(int Start, int End)> regions, int pos)
    {
        int lo = 0, hi = regions.Count;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (regions[mid].End > pos) hi = mid; else lo = mid + 1;
        }
        return lo;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (line.Length == 0) return;
        var p = IsDark ? Dark : Light;
        var triples = GetTriples();
        int lineStart = line.Offset, lineEnd = line.EndOffset;

        // 1) triple-quoted string parts that touch this line
        for (var k = FirstRegionEndingAfter(triples, lineStart); k < triples.Count && triples[k].Start < lineEnd; k++)
        {
            var a = Math.Max(triples[k].Start, lineStart);
            var b = Math.Min(triples[k].End, lineEnd);
            if (b > a) ChangeLinePart(a, b, e => e.TextRunProperties.SetForegroundBrush(p.String));
        }

        // 2) ordinary tokens (skipped when they start inside a triple-quoted string)
        var text = CurrentContext.Document.GetText(line);
        string? previousIdent = null;
        foreach (Match m in TokenRegex.Matches(text))
        {
            var abs = lineStart + m.Index;
            var ri = FirstRegionEndingAfter(triples, abs);
            if (ri < triples.Count && triples[ri].Start <= abs) continue;

            IBrush? brush = null;
            var isIdent = false;
            if (m.Groups["comment"].Success) brush = p.Comment;
            else if (m.Groups["string"].Success) brush = p.String;
            else if (m.Groups["number"].Success) brush = p.Number;
            else if (m.Groups["deco"].Success) brush = p.Decorator;
            else
            {
                isIdent = true;
                brush = Classify(m.Value, previousIdent, p);
            }
            previousIdent = isIdent ? m.Value : null;

            if (brush is null) continue;
            var chosen = brush;
            ChangeLinePart(abs, abs + m.Length, e => e.TextRunProperties.SetForegroundBrush(chosen));
        }
    }

    private static IBrush? Classify(string id, string? previous, Palette p)
    {
        if (Keywords.Contains(id)) return p.Keyword;
        if (id is "self" or "cls") return p.SelfRef;
        if (previous == "def") return p.Function;
        if (previous == "class") return p.Class;
        if (Builtins.Contains(id)) return p.Builtin;
        return null;
    }
}
