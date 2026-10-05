using System.Text.Json;
using ISI.IDE.Models;

namespace ISI.IDE.Services;

/// <summary>pip operations for the package manager window.</summary>
public static class PipService
{
    private static readonly string[] Common = { "-m", "pip", "--disable-pip-version-check" };

    public static async Task<(List<PackageItem> Items, string Error)> ListAsync(string python)
    {
        var r = await ProcessUtil.RunAsync(python, Common.Concat(new[] { "list", "--format=json" }));
        if (r.ExitCode != 0) return (new List<PackageItem>(), r.StdErr.Trim());
        try
        {
            using var doc = JsonDocument.Parse(r.StdOut);
            var items = doc.RootElement.EnumerateArray()
                .Select(e => new PackageItem(
                    e.GetProperty("name").GetString() ?? "",
                    e.GetProperty("version").GetString() ?? ""))
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return (items, "");
        }
        catch (JsonException ex)
        {
            return (new List<PackageItem>(), ex.Message);
        }
    }

    public static Task<ProcessResult> InstallAsync(string python, string requirement) =>
        ProcessUtil.RunAsync(python, Common.Concat(new[] { "install", requirement }));

    public static Task<ProcessResult> UninstallAsync(string python, string name) =>
        ProcessUtil.RunAsync(python, Common.Concat(new[] { "uninstall", "-y", name }));
}
