using System.Windows.Controls;

namespace YAvuzeli.Client.Views;

public partial class TeachersView : UserControl
{
    private readonly ViewModels.TeachersViewModel _model = new();
    private bool _loaded;
    public TeachersView()
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
