using System.Windows.Controls;

namespace YAvuzeli.Client.Views;

public partial class ScheduleView : UserControl
{
    private readonly ViewModels.ProgramViewModel _model = new();
    private bool _loaded;
    public ScheduleView()
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
