using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using YAvuzeli.Client.Services;
using YAvuzeli.Shared.Students;

namespace YAvuzeli.Client.ViewModels;

public sealed class StudentsViewModel : BaseViewModel
{
    private readonly StudentStore _store = new();
    public string StorageText => $"Veriler bu bilgisayarda saklanır: {_store.DatabasePath}";
    private readonly ObservableCollection<StudentDto> _students = [];
    private static readonly CompareInfo SearchCulture = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;
    private Guid? _editingId;
    private bool _populating;
    private bool _dirty;
    private bool _busy;
    private string _search = "";
    private string _message = "";
    private string _firstName = "";
    private string _lastName = "";
    private string _email = "";
    private string _phone = "";
    private DateTime? _birthDate;
    private bool _active = true;
    private StudentDto? _selected;

    public ICollectionView Students { get; }
    public string CountText => $"{Students.Cast<object>().Count()} / {_students.Count} öğrenci";
    public string TotalStudentsText => _students.Count.ToString("N0", CultureInfo.GetCultureInfo("tr-TR"));
    public string ActiveStudentsText => _students.Count(x => x.IsActive).ToString("N0", CultureInfo.GetCultureInfo("tr-TR"));
    public string InactiveStudentsText => _students.Count(x => !x.IsActive).ToString("N0", CultureInfo.GetCultureInfo("tr-TR"));
    public string SelectedStudentText => SelectedStudent is null
        ? "Tablodan bir öğrenci seçin."
        : $"{SelectedStudent.FirstName} {SelectedStudent.LastName} seçili.";
    public string EditorTitle => _editingId.HasValue ? "Öğrenciyi düzenle" : "Yeni öğrenci";
    public string SaveLabel => _editingId.HasValue ? "Değişiklikleri kaydet" : "Öğrenciyi kaydet";
    public bool IsBusy { get => _busy; private set { _busy = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(IsReady)); CommandManager.InvalidateRequerySuggested(); } }
    public bool IsReady => !IsBusy;
    public string Message { get => _message; private set { _message = value; RaisePropertyChanged(); } }
    public string SearchText { get => _search; set { _search = value; RaisePropertyChanged(); Students.Refresh(); RaisePropertyChanged(nameof(CountText)); } }
    public StudentDto? SelectedStudent
    {
        get => _selected;
        set
        {
            _selected = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(SelectedStudentText));
            CommandManager.InvalidateRequerySuggested();
        }
    }
    public string FirstName { get => _firstName; set { _firstName = value; Changed(nameof(FirstName)); } }
    public string LastName { get => _lastName; set { _lastName = value; Changed(nameof(LastName)); } }
    public string Email { get => _email; set { _email = value; Changed(nameof(Email)); } }
    public string Phone { get => _phone; set { _phone = value; Changed(nameof(Phone)); } }
    public DateTime? BirthDate { get => _birthDate; set { _birthDate = value; Changed(nameof(BirthDate)); } }
    public bool IsActive { get => _active; set { _active = value; Changed(nameof(IsActive)); } }

    public ICommand RefreshCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand NewCommand { get; }

    public StudentsViewModel()
    {
        Students = CollectionViewSource.GetDefaultView(_students);
        Students.Filter = item => item is StudentDto s &&
            (string.IsNullOrWhiteSpace(SearchText) || SearchCulture.IndexOf(
                $"{s.FirstName} {s.LastName} {s.Email} {s.Phone}", SearchText.Trim(), CompareOptions.IgnoreCase) >= 0);
        RefreshCommand = Async(() => RunAsync(async () => { await RefreshAsync(); Message = "Liste güncellendi."; }));
        SaveCommand = Async(() => RunAsync(SaveAsync));
        DeleteCommand = Async(() => RunAsync(DeleteAsync), () => SelectedStudent is not null);
        EditCommand = new RelayCommand(_ => { if (ConfirmDiscard()) Populate(SelectedStudent); }, _ => IsReady && SelectedStudent is not null);
        NewCommand = new RelayCommand(_ => { if (ConfirmDiscard()) Populate(null); }, _ => IsReady);
    }

    public Task LoadAsync() => RunAsync(RefreshAsync);
    public bool ConfirmDiscard() => !_dirty || MessageBox.Show(
        "Kaydedilmemiş değişiklikler var. Bu değişikliklerden vazgeçilsin mi?", "YAvuzeli",
        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;

    private ICommand Async(Func<Task> action, Func<bool>? enabled = null) =>
        new AsyncRelayCommand(action, () => IsReady && (enabled?.Invoke() ?? true), ShowError);

    private void Changed(string name)
    {
        if (!_populating) _dirty = true;
        RaisePropertyChanged(name);
    }

    private void Populate(StudentDto? student)
    {
        _populating = true;
        _editingId = student?.Id;
        FirstName = student?.FirstName ?? "";
        LastName = student?.LastName ?? "";
        Email = student?.Email ?? "";
        Phone = student?.Phone ?? "";
        BirthDate = student?.DateOfBirth.ToDateTime(TimeOnly.MinValue);
        IsActive = student?.IsActive ?? true;
        _populating = false;
        _dirty = false;
        Message = "";
        RaisePropertyChanged(nameof(EditorTitle));
        RaisePropertyChanged(nameof(SaveLabel));
    }

    private async Task RefreshAsync()
    {
        // Fetch before changing the collection so a connection error retains the old list.
        var items = await _store.GetAllAsync();
        var selectedId = SelectedStudent?.Id;
        _students.Clear();
        foreach (var item in items) _students.Add(item);
        SelectedStudent = _students.FirstOrDefault(x => x.Id == selectedId);
        NotifySummaryChanged();
        Message = items.Count == 0 ? "Henüz öğrenci yok. Sağdaki formdan ilk öğrenciyi ekleyebilirsiniz." : "";
    }

    private async Task SaveAsync()
    {
        var input = new StudentInput
        {
            FirstName = FirstName.Trim(), LastName = LastName.Trim(),
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
            DateOfBirth = BirthDate.HasValue ? DateOnly.FromDateTime(BirthDate.Value) : null,
            IsActive = IsActive
        };
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true))
        {
            Message = string.Join("\n", errors.Select(x => x.ErrorMessage));
            return;
        }
        var saved = await _store.SaveAsync(_editingId, input);
        // Use the acknowledged response, avoiding a second request that might mask a successful create.
        var old = _students.FirstOrDefault(x => x.Id == saved.Id);
        if (old is not null) _students.Remove(old);
        _students.Add(saved);
        SelectedStudent = saved;
        Populate(saved);
        NotifySummaryChanged();
        Message = "Öğrenci kaydedildi.";
    }

    private async Task DeleteAsync()
    {
        var student = SelectedStudent;
        if (student is null) return;
        if (MessageBox.Show($"{student.FirstName} {student.LastName} kalıcı olarak silinsin mi?",
            "Öğrenciyi sil", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        await _store.DeleteAsync(student.Id);
        _students.Remove(student);
        SelectedStudent = null;
        if (_editingId == student.Id) Populate(null);
        NotifySummaryChanged();
        Message = "Öğrenci silindi.";
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        Message = "İşlem yapılıyor…";
        try { await action(); }
        catch (Exception error) { ShowError(error); }
        finally { IsBusy = false; CommandManager.InvalidateRequerySuggested(); }
    }

    private void NotifySummaryChanged()
    {
        Students.Refresh();
        RaisePropertyChanged(nameof(CountText));
        RaisePropertyChanged(nameof(TotalStudentsText));
        RaisePropertyChanged(nameof(ActiveStudentsText));
        RaisePropertyChanged(nameof(InactiveStudentsText));
    }

    private void ShowError(Exception error) => Message = error switch
    {
        KeyNotFoundException => "Öğrenci bulunamadı. Listeyi yenileyin.",
        DbUpdateException { InnerException: SqliteException { SqliteExtendedErrorCode: 787 or 1811 } }
            => "Öğrencinin bağlı kayıtları var. Silmek yerine pasif yapabilirsiniz.",
        DbUpdateException or SqliteException => "Veritabanı işlemi tamamlanamadı. Dosyanın yazılabilir olduğunu kontrol edip tekrar deneyin.",
        IOException or UnauthorizedAccessException => "Yerel veri klasörüne erişilemedi. Klasör izinlerini kontrol edin.",
        _ => error.Message
    };
}
