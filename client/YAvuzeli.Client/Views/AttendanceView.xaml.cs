using System.Windows.Controls;

namespace YAvuzeli.Client.Views;

public partial class AttendanceView : UserControl
{
    private readonly ViewModels.AttendanceViewModel _model = new();
    private bool _loaded;
    public AttendanceView()
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
