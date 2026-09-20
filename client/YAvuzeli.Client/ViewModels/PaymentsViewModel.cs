using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using YAvuzeli.Client.Services;

namespace YAvuzeli.Client.ViewModels;

public sealed class PaymentsViewModel : BaseViewModel
{
    private readonly SchoolStore _store = new();
    private Guid? _editingId;
    private PaymentRow? _selected;
    private string _message = "";
    private bool _busy;
    public ObservableCollection<PaymentRow> Payments { get; } = [];
    public ObservableCollection<LookupRow> Students { get; } = [];
    public Guid StudentId { get; set; }
    public string Amount { get; set; } = "";
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string PaymentMethod { get; set; } = "Nakit";
    public string Note { get; set; } = "";
    public string TotalText => Payments.Sum(x => x.Amount).ToString("C0", CultureInfo.GetCultureInfo("tr-TR"));
    public string Message { get => _message; set { _message = value; RaisePropertyChanged(); } }
    public bool IsReady => !_busy;
    public PaymentRow? SelectedPayment { get => _selected; set { _selected = value; RaisePropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
    public string SaveLabel => _editingId.HasValue ? "Değişiklikleri kaydet" : "Ödemeyi kaydet";
    public ICommand RefreshCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand DeleteCommand { get; }

    public PaymentsViewModel()
    {
        RefreshCommand = Async(LoadAsync);
        SaveCommand = Async(SaveAsync);
        EditCommand = new RelayCommand(_ => Populate(SelectedPayment), _ => SelectedPayment is not null);
        NewCommand = new RelayCommand(_ => Populate(null));
        DeleteCommand = Async(DeleteAsync, () => SelectedPayment is not null);
    }

    public async Task LoadAsync() => await RunAsync(async () =>
    {
        Students.Clear();
        foreach (var student in await _store.GetStudentLookupsAsync()) Students.Add(student);
        Payments.Clear();
        foreach (var item in await _store.GetPaymentsAsync()) Payments.Add(item);
        RaisePropertyChanged(nameof(TotalText));
        Message = Students.Count == 0 ? "Ödeme eklemek için önce öğrenci kaydı oluşturun." : "";
    });

    private async Task SaveAsync() => await RunAsync(async () =>
    {
        if (!decimal.TryParse(Amount, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out var amount) &&
            !decimal.TryParse(Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
            throw new InvalidOperationException("Geçerli bir tutar girin.");
        var saved = await _store.SavePaymentAsync(_editingId, StudentId, amount, PaymentDate, PaymentMethod, Note);
        Replace(Payments, saved, x => x.Id == saved.Id);
        SelectedPayment = saved;
        Populate(saved);
        RaisePropertyChanged(nameof(TotalText));
        Message = "Ödeme kaydedildi.";
    });

    private async Task DeleteAsync()
    {
        if (SelectedPayment is not { } payment) return;
        if (MessageBox.Show($"{payment.StudentName} ödemesi silinsin mi?", "Ödemeyi sil",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await RunAsync(async () =>
        {
            await _store.DeletePaymentAsync(payment.Id);
            Payments.Remove(payment);
            Populate(null);
            RaisePropertyChanged(nameof(TotalText));
            Message = "Ödeme silindi.";
        });
    }

    private void Populate(PaymentRow? payment)
    {
        _editingId = payment?.Id;
        StudentId = payment?.StudentId ?? Guid.Empty;
        Amount = payment?.Amount.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR")) ?? "";
        PaymentDate = payment?.PaymentDate ?? DateTime.Today;
        PaymentMethod = payment?.PaymentMethod ?? "Nakit";
        Note = payment?.Note ?? "";
        RaisePropertyChanged(nameof(StudentId)); RaisePropertyChanged(nameof(Amount));
        RaisePropertyChanged(nameof(PaymentDate)); RaisePropertyChanged(nameof(PaymentMethod));
        RaisePropertyChanged(nameof(Note)); RaisePropertyChanged(nameof(SaveLabel));
    }

    private ICommand Async(Func<Task> action, Func<bool>? canExecute = null) =>
        new AsyncRelayCommand(action, () => IsReady && (canExecute?.Invoke() ?? true), e => Message = e.Message);

    private async Task RunAsync(Func<Task> action)
    {
        if (_busy) return;
        _busy = true; RaisePropertyChanged(nameof(IsReady)); CommandManager.InvalidateRequerySuggested();
        try { await action(); } catch (Exception e) { Message = e.Message; }
        finally { _busy = false; RaisePropertyChanged(nameof(IsReady)); CommandManager.InvalidateRequerySuggested(); }
    }

    private static void Replace<T>(ObservableCollection<T> list, T item, Func<T, bool> match)
    {
        var old = list.FirstOrDefault(match);
        if (old is not null) list.Remove(old);
        list.Add(item);
    }
}
