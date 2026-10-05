using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using ISI.IDE.Models;

namespace ISI.IDE.Services;

/// <summary>P/Invoke bindings for the C++ core (isi_core.dll / libisi_core.so / libisi_core.dylib).</summary>
internal static class NativeMethods
{
    private const string Lib = "isi_core";

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr isi_version();

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int isi_abi_version();

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr isi_analyze_json([MarshalAs(UnmanagedType.LPUTF8Str)] string source);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void isi_free(IntPtr ptr);
}

/// <summary>
/// Code intelligence facade. Uses the native C++ scanner when available and silently
/// falls back to a small managed implementation (outline only) otherwise.
/// </summary>
public static class CodeIntelligence
{
    private const int ExpectedAbi = 1;
    private static bool _nativeBroken;

    public static string? NativeVersion { get; }

    static CodeIntelligence()
    {
        try
        {
            if (NativeMethods.isi_abi_version() == ExpectedAbi)
                NativeVersion = Marshal.PtrToStringUTF8(NativeMethods.isi_version());
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            NativeVersion = null;
        }
    }

    public static bool IsNativeAvailable => NativeVersion is not null && !_nativeBroken;

    public static AnalysisResult Analyze(string source)
    {
        if (IsNativeAvailable)
        {
            try
            {
                var ptr = NativeMethods.isi_analyze_json(source);
                if (ptr != IntPtr.Zero)
                {
                    try { return Parse(Marshal.PtrToStringUTF8(ptr) ?? "{}"); }
                    finally { NativeMethods.isi_free(ptr); }
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or JsonException)
            {
                _nativeBroken = true;
            }
        }
        return AnalyzeManaged(source);
    }

    private static AnalysisResult Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var symbols = new List<SymbolItem>();
        var diagnostics = new List<DiagnosticItem>();
        foreach (var e in doc.RootElement.GetProperty("symbols").EnumerateArray())
            symbols.Add(new SymbolItem(
                e.GetProperty("kind").GetString() ?? "",
                e.GetProperty("name").GetString() ?? "",
                e.GetProperty("line").GetInt32(),
                e.GetProperty("indent").GetInt32()));
        foreach (var e in doc.RootElement.GetProperty("diagnostics").EnumerateArray())
            diagnostics.Add(new DiagnosticItem(
                e.GetProperty("severity").GetString() ?? "warning",
                e.GetProperty("message").GetString() ?? "",
                e.GetProperty("line").GetInt32(),
                e.GetProperty("column").GetInt32()));
        return new AnalysisResult(symbols, diagnostics);
    }

    private static readonly Regex DefRegex = new(
        @"^(?<indent>[ \t]*)(?:async[ \t]+)?(?<kw>def|class)[ \t]+(?<name>\w+)",
        RegexOptions.Compiled);

    private static AnalysisResult AnalyzeManaged(string source)
    {
        var symbols = new List<SymbolItem>();
        var lines = source.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var m = DefRegex.Match(lines[i]);
            if (m.Success)
                symbols.Add(new SymbolItem(
                    m.Groups["kw"].Value == "class" ? "class" : "function",
                    m.Groups["name"].Value, i + 1, m.Groups["indent"].Length));
        }
        return new AnalysisResult(symbols, new List<DiagnosticItem>());
    }
}
