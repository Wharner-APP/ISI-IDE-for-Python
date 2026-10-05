using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace ISI.IDE.Views;

/// <summary>Small code-built dialog: message + optional text input + buttons. Closing with X returns null.</summary>
public sealed class MessageDialog : Window
{
    public sealed record Result(int ButtonIndex, string Text);

    private readonly TextBox? _input;

    public MessageDialog(string title, string message, string[] buttons, bool withInput = false, string initial = "")
    {
        Title = title;
        Width = 480;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Icon = Branding.CreateIcon();

        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 14 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });

        if (withInput)
        {
            _input = new TextBox { Text = initial };
            panel.Children.Add(_input);
        }

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
        };
        for (var i = 0; i < buttons.Length; i++)
        {
            var index = i;
            var button = new Button { Content = buttons[i], MinWidth = 90 };
            if (i == 0) button.Classes.Add("accent");
            button.Click += (_, _) => Close(new Result(index, _input?.Text ?? ""));
            row.Children.Add(button);
        }
        panel.Children.Add(row);
        Content = panel;

        Opened += (_, _) => _input?.Focus();
    }

    public static Task<Result?> ShowAsync(Window owner, string title, string message, string[] buttons,
                                          bool withInput = false, string initial = "") =>
        new MessageDialog(title, message, buttons, withInput, initial).ShowDialog<Result?>(owner);
}
