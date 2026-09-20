using System.Windows;

namespace YAvuzeli.Client;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ViewModels.MainViewModel();
        Closing += (_, args) => args.Cancel = !((ViewModels.MainViewModel)DataContext).CanClose();
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
