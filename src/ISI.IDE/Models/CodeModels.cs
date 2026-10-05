namespace ISI.IDE.Models;

public sealed record SymbolItem(string Kind, string Name, int Line, int Indent)
{
    public string Display => $"{new string(' ', Math.Min(Indent, 16))}{(Kind == "class" ? "C" : "f")}  {Name}";
}

public sealed record DiagnosticItem(string Severity, string Message, int Line, int Column)
{
    public string Display => $"{(Severity == "error" ? "[error]  " : "[warn]   ")}Ln {Line}, Col {Column}   {Message}";
}

public sealed record AnalysisResult(IReadOnlyList<SymbolItem> Symbols, IReadOnlyList<DiagnosticItem> Diagnostics)
{
    public static AnalysisResult Empty { get; } = new(Array.Empty<SymbolItem>(), Array.Empty<DiagnosticItem>());
}

public sealed record PackageItem(string Name, string Version);
