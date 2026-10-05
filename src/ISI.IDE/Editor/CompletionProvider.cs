using System.Text.RegularExpressions;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace ISI.IDE.Editor;

/// <summary>Context-free completion: keywords, builtins and identifiers found in the current file.</summary>
public static class CompletionProvider
{
    private static readonly Regex IdentifierRegex = new(@"[A-Za-z_]\w{2,}", RegexOptions.Compiled);
    private static readonly Regex MemberRegex = new(@"\.([A-Za-z_]\w*)", RegexOptions.Compiled);

    public static List<ICompletionData> Get(string documentText, string prefix, bool afterDot)
    {
        var items = new Dictionary<string, string>(StringComparer.Ordinal);   // text -> kind

        if (afterDot)
        {
            foreach (Match m in MemberRegex.Matches(documentText)) items.TryAdd(m.Groups[1].Value, "member");
        }
        else
        {
            foreach (var k in PythonColorizer.Keywords) items.TryAdd(k, "keyword");
            foreach (var b in PythonColorizer.Builtins) items.TryAdd(b, "builtin");
            foreach (Match m in IdentifierRegex.Matches(documentText)) items.TryAdd(m.Value, "name");
        }

        return items
            .Where(kv => kv.Key.Length > prefix.Length &&
                         kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(200)
            .Select(kv => (ICompletionData)new PythonCompletionData(kv.Key, kv.Value))
            .ToList();
    }
}

public sealed class PythonCompletionData : ICompletionData
{
    public PythonCompletionData(string text, string kind)
    {
        Text = text;
        Description = kind;
    }

    public IImage? Image => null;
    public string Text { get; }
    public object Content => Text;
    public object Description { get; }
    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs) =>
        textArea.Document.Replace(completionSegment, Text);
}
