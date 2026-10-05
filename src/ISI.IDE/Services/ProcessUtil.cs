using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace ISI.IDE.Services;

public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);

public static class ProcessUtil
{
    /// <summary>Runs a process to completion and captures its output (git, pip, linters...).</summary>
    public static async Task<ProcessResult> RunAsync(
        string file, IEnumerable<string> args, string? workingDirectory = null,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        if (workingDirectory is not null) psi.WorkingDirectory = workingDirectory;
        foreach (var a in args) psi.ArgumentList.Add(a);
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        psi.Environment["GIT_TERMINAL_PROMPT"] = "0";   // never hang on a credentials prompt

        try
        {
            using var p = Process.Start(psi)!;
            var stdout = p.StandardOutput.ReadToEndAsync(ct);
            var stderr = p.StandardError.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);
            return new ProcessResult(p.ExitCode, await stdout, await stderr);
        }
        catch (Win32Exception ex)
        {
            return new ProcessResult(-1, "", $"Cannot start '{file}': {ex.Message}");
        }
    }
}

/// <summary>Runs a long-lived child process (a Python script) and streams its output.</summary>
public sealed class ScriptRunner : IDisposable
{
    private Process? _process;

    public event Action<string>? OutputReceived;
    public event Action<int>? Exited;

    public bool IsRunning => _process is { HasExited: false };

    public void Start(string executable, IEnumerable<string> args, string workingDirectory,
                      IDictionary<string, string>? environment = null)
    {
        if (IsRunning) throw new InvalidOperationException("A process is already running.");

        var psi = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        psi.Environment["PYTHONUNBUFFERED"] = "1";
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        if (environment is not null)
            foreach (var (k, v) in environment) psi.Environment[k] = v;

        var p = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null.");
        _process = p;

        _ = Task.Run(async () =>
        {
            var o = PumpAsync(p.StandardOutput);
            var e = PumpAsync(p.StandardError);
            await p.WaitForExitAsync();
            await Task.WhenAll(o, e);   // flush everything before reporting the exit code
            Exited?.Invoke(p.ExitCode);
        });
    }

    private async Task PumpAsync(StreamReader reader)
    {
        var buffer = new char[4096];
        try
        {
            int n;
            while ((n = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                OutputReceived?.Invoke(new string(buffer, 0, n));
        }
        catch (IOException) { /* stream closed */ }
    }

    public void WriteInput(string text)
    {
        try { _process?.StandardInput.WriteLine(text); }
        catch (IOException) { }
        catch (InvalidOperationException) { }
    }

    public void Stop()
    {
        try { if (IsRunning) _process!.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
    }

    public void Dispose() => Stop();
}
