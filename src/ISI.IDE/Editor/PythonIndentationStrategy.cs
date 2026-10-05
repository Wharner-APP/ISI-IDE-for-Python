using System.Text.RegularExpressions;
using AvaloniaEdit.Document;
using AvaloniaEdit.Indentation;

namespace ISI.IDE.Editor;

/// <summary>Keeps the previous line's indentation, adds a level after ':' and removes one after return/pass/break...</summary>
public sealed class PythonIndentationStrategy : IIndentationStrategy
{
    private const string Unit = "    ";
    private static readonly Regex Dedent = new(@"^(return|pass|break|continue|raise)\b", RegexOptions.Compiled);

    public void IndentLine(TextDocument document, DocumentLine line)
    {
        if (line.PreviousLine is null) return;
        var prev = document.GetText(line.PreviousLine);

        var n = 0;
        while (n < prev.Length && (prev[n] == ' ' || prev[n] == '\t')) n++;
        var indent = prev[..n];

        var code = StripComment(prev).TrimEnd();
        if (code.EndsWith(':'))
            indent += Unit;
        else if (Dedent.IsMatch(code.TrimStart()) && indent.EndsWith(Unit, StringComparison.Ordinal))
            indent = indent[..^Unit.Length];

        var current = document.GetText(line);
        var m = 0;
        while (m < current.Length && char.IsWhiteSpace(current[m])) m++;
        document.Replace(line.Offset, m, indent);
    }

    public void IndentLines(TextDocument document, int beginLine, int endLine)
    {
        for (var l = beginLine; l <= endLine; l++)
            IndentLine(document, document.GetLineByNumber(l));
    }

    private static string StripComment(string line)
    {
        var hash = line.IndexOf('#');
        if (hash < 0) return line;
        // Naive but safe: only cut when no quote precedes the '#'.
        var before = line[..hash];
        return before.Contains('"') || before.Contains('\'') ? line : before;
    }
}
