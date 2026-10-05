using System.Diagnostics;

namespace ISI.IDE.Services;

public static class Os
{
    public static void OpenUrl(string url)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", url);
            else
                Process.Start("xdg-open", url);
        }
        catch
        {
            // No browser/handler available - nothing sensible to do.
        }
    }
}
