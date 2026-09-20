using System.Windows.Controls;

namespace YAvuzeli.Client.Views;

public partial class CoursesView : UserControl
{
    private readonly ViewModels.CoursesViewModel _model = new();
    private bool _loaded;
    public CoursesView()
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
