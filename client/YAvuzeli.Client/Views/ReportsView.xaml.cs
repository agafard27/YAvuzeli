using System.Windows.Controls;

namespace YAvuzeli.Client.Views;

public partial class ReportsView : UserControl
{
    private readonly ViewModels.ReportsViewModel _model = new();
    private bool _loaded;
    public ReportsView()
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
