using System.Windows.Controls;

namespace YAvuzeli.Client.Views;

public partial class DashboardView : UserControl
{
    private readonly ViewModels.DashboardViewModel _model = new();
    private bool _loaded;
    public DashboardView()
    {
        InitializeComponent();
        DataContext = _model;
        Loaded += async (_, _) =>
        {
            if (_loaded) return;
            _loaded = true;
            await _model.LoadAsync();
        };
    }
}
