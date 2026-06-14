using Avalonia.Controls;
using Launcher.ViewModels;

namespace Launcher.Views;

public partial class ServerLauncherView : UserControl
{
    public ServerLauncherView()
    {
        InitializeComponent();
        DataContext = new ServerLauncherViewModel();
    }
}
