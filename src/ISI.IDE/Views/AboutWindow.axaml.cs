using Avalonia.Controls;
using Avalonia.Interactivity;
using ISI.IDE.Services;

namespace ISI.IDE.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        Icon = Branding.CreateIcon();
        Logo.Source = Branding.LoadLogo();
        VersionText.Text = $"Version {Branding.Version}";
        CoreText.Text = CodeIntelligence.IsNativeAvailable
            ? $"C++ core {CodeIntelligence.NativeVersion}  ·  .NET {Environment.Version}"
            : $"C++ core not loaded  ·  .NET {Environment.Version}";
    }

    private void OnLinkClick(object? sender, RoutedEventArgs e) => Os.OpenUrl(Branding.Website);
    private void OnOkClick(object? sender, RoutedEventArgs e) => Close();
}
