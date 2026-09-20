using System.Windows.Controls;

namespace YAvuzeli.Client.Views;

public partial class StudentsView : UserControl
{
    public ViewModels.StudentsViewModel Model { get; } = new();
    private bool _loaded;
    public StudentsView()
    {
        InitializeComponent();
        DataContext = Model;
        Loaded += async (_, _) =>
        {
            if (_loaded) return;
            _loaded = true;
            await Model.LoadAsync();
        };
    }
}
