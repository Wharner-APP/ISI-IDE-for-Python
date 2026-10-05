using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using ISI.IDE.Services;
using ISI.IDE.ViewModels;
using ISI.IDE.Views;

namespace ISI.IDE;

public partial class App : Application
{
    public override void Initialize()
    {
        AppSettings.Load();
        AvaloniaXamlLoader.Load(this);
        ApplyTheme(AppSettings.Current.Theme);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Splash first, main window right after (startup target: < 3 s).
            var splash = new SplashWindow();
            desktop.MainWindow = splash;
            splash.Opened += async (_, _) =>
            {
                await Task.Delay(700);
                var main = new MainWindow();
                main.DataContext = new MainWindowViewModel(main);
                desktop.MainWindow = main;
                main.Show();
                splash.Close();
            };
        }
        base.OnFrameworkInitializationCompleted();
    }

    public static void ApplyTheme(string theme)
    {
        if (Current is null) return;
        Current.RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "System" => ThemeVariant.Default,
            _ => ThemeVariant.Dark,
        };
    }
}
