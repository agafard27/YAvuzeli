using System.Windows.Controls;

namespace YAvuzeli.Client.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = new ViewModels.SettingsViewModel();
    }
}
