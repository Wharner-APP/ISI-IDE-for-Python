using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ISI.IDE;

/// <summary>Product identity shown in the About dialog, splash screen and title bar.</summary>
public static class Branding
{
    public const string ProductName = "ISI IDE for Python";
    public const string Copyright = "© Wharner Group";
    public const string Developer = "Developed by Wharner APP";
    public const string Website = "https://wharner-official-app.tilda.ws/isiidepython";
    public const string Version = "0.1.0";

    // Palette (see docs/BRANDING.md)
    public const string NavyHex = "#0F172A";
    public const string BlueHex = "#2563EB";
    public const string TealHex = "#14B8A6";
    public const string YellowHex = "#FBBF24";

    public static Bitmap LoadLogo() => Load("logo.png");

    public static WindowIcon CreateIcon() => new(Load("icon.png"));

    private static Bitmap Load(string name)
    {
        var uri = new Uri($"avares://{typeof(Branding).Assembly.GetName().Name}/Assets/{name}");
        using var stream = AssetLoader.Open(uri);
        return new Bitmap(stream);
    }
}
