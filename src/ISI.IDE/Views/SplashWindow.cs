using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ISI.IDE.Views;

/// <summary>Borderless splash screen shown while the main window is created.</summary>
public sealed class SplashWindow : Window
{
    public SplashWindow()
    {
        Width = 520;
        Height = 300;
        CanResize = false;
        ShowInTaskbar = false;
        SystemDecorations = SystemDecorations.None;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.Parse(Branding.NavyHex));
        Icon = Branding.CreateIcon();

        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 8,
        };
        stack.Children.Add(new Image { Source = Branding.LoadLogo(), Width = 120, Height = 120 });
        stack.Children.Add(new TextBlock
        {
            Text = Branding.ProductName,
            FontSize = 26,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"{Branding.Copyright}  ·  {Branding.Developer}",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        stack.Children.Add(new ProgressBar { IsIndeterminate = true, Width = 220, Height = 4, Margin = new Thickness(0, 12, 0, 0) });
        Content = stack;
    }
}
