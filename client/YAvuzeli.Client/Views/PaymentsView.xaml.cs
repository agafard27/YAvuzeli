using System.Windows.Controls;

namespace YAvuzeli.Client.Views;

public partial class PaymentsView : UserControl
{
    private readonly ViewModels.PaymentsViewModel _model = new();
    private bool _loaded;
    public PaymentsView()
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
