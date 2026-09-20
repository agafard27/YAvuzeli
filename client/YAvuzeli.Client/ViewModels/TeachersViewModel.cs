using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using YAvuzeli.Client.Services;

namespace YAvuzeli.Client.ViewModels;

public sealed class TeachersViewModel : BaseViewModel
{
    private readonly SchoolStore _store = new();
    private Guid? _editingId;
    private TeacherRow? _selected;
    private bool _busy;
    private string _message = "";
    public ObservableCollection<TeacherRow> Teachers { get; } = [];
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string Message { get => _message; set { _message = value; RaisePropertyChanged(); } }
    public bool IsReady => !_busy;
    public string SaveLabel => _editingId.HasValue ? "Değişiklikleri kaydet" : "Öğretmeni kaydet";
    public TeacherRow? SelectedTeacher { get => _selected; set { _selected = value; RaisePropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
    public ICommand RefreshCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand DeleteCommand { get; }

    public TeachersViewModel()
    {
        RefreshCommand = Async(LoadAsync);
        SaveCommand = Async(SaveAsync);
        EditCommand = new RelayCommand(_ => Populate(SelectedTeacher), _ => SelectedTeacher is not null);
        NewCommand = new RelayCommand(_ => Populate(null));
        DeleteCommand = Async(DeleteAsync, () => SelectedTeacher is not null);
    }

    public async Task LoadAsync() => await RunAsync(async () =>
    {
        Teachers.Clear();
        foreach (var item in await _store.GetTeachersAsync()) Teachers.Add(item);
        Message = Teachers.Count == 0 ? "Henüz öğretmen kaydı yok." : "";
    });

    private async Task SaveAsync() => await RunAsync(async () =>
    {
        var saved = await _store.SaveTeacherAsync(_editingId, FirstName, LastName, Phone, Email, IsActive);
        Replace(Teachers, saved, x => x.Id == saved.Id);
        SelectedTeacher = saved;
        Populate(saved);
        Message = "Öğretmen kaydedildi.";
    });

    private async Task DeleteAsync()
    {
        if (SelectedTeacher is not { } teacher) return;
        if (MessageBox.Show($"{teacher.FirstName} {teacher.LastName} silinsin mi?", "Öğretmeni sil",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await RunAsync(async () =>
        {
            await _store.DeleteTeacherAsync(teacher.Id);
            Teachers.Remove(teacher);
            Populate(null);
            Message = "Öğretmen silindi.";
        });
    }

    private void Populate(TeacherRow? teacher)
    {
        _editingId = teacher?.Id;
        FirstName = teacher?.FirstName ?? "";
        LastName = teacher?.LastName ?? "";
        Phone = teacher?.Phone ?? "";
        Email = teacher?.Email ?? "";
        IsActive = teacher?.IsActive ?? true;
        RaiseAll();
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

    private void RaiseAll()
    {
        RaisePropertyChanged(nameof(FirstName)); RaisePropertyChanged(nameof(LastName));
        RaisePropertyChanged(nameof(Phone)); RaisePropertyChanged(nameof(Email));
        RaisePropertyChanged(nameof(IsActive)); RaisePropertyChanged(nameof(SaveLabel));
    }

    private static void Replace<T>(ObservableCollection<T> list, T item, Func<T, bool> match)
    {
        var old = list.FirstOrDefault(match);
        if (old is not null) list.Remove(old);
        list.Add(item);
    }
}
