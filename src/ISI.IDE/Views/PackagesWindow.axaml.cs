using Avalonia.Controls;
using ISI.IDE.ViewModels;

namespace ISI.IDE.Views;

public partial class PackagesWindow : Window
{
    public PackagesWindow() : this("python") { }

    public PackagesWindow(string pythonPath)
    {
        InitializeComponent();
        Icon = Branding.CreateIcon();
        var vm = new PackagesViewModel(pythonPath);
        DataContext = vm;
        Opened += async (_, _) => await vm.RefreshCommand.ExecuteAsync(null);
    }
}
