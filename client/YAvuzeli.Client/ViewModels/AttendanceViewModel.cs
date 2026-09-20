using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using YAvuzeli.Client.Services;

namespace YAvuzeli.Client.ViewModels;

public sealed class AttendanceViewModel : BaseViewModel
{
    private readonly SchoolStore _store = new();
    private Guid? _editingId;
    private AttendanceRow? _selected;
    private string _message = "";
    private bool _busy;
    public ObservableCollection<AttendanceRow> Attendances { get; } = [];
    public ObservableCollection<LookupRow> Students { get; } = [];
    public ObservableCollection<LookupRow> Courses { get; } = [];
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public DateTime AttendanceDate { get; set; } = DateTime.Today;
    public bool IsPresent { get; set; } = true;
    public string PresentRateText => Attendances.Count == 0 ? "%0" : $"%{Attendances.Count(x => x.IsPresent) * 100 / Attendances.Count}";
    public string Message { get => _message; set { _message = value; RaisePropertyChanged(); } }
    public bool IsReady => !_busy;
    public AttendanceRow? SelectedAttendance { get => _selected; set { _selected = value; RaisePropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
    public string SaveLabel => _editingId.HasValue ? "Değişiklikleri kaydet" : "Yoklamayı kaydet";
    public ICommand RefreshCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand DeleteCommand { get; }

    public AttendanceViewModel()
    {
        RefreshCommand = Async(LoadAsync);
        SaveCommand = Async(SaveAsync);
        EditCommand = new RelayCommand(_ => Populate(SelectedAttendance), _ => SelectedAttendance is not null);
        NewCommand = new RelayCommand(_ => Populate(null));
        DeleteCommand = Async(DeleteAsync, () => SelectedAttendance is not null);
    }

    public async Task LoadAsync() => await RunAsync(async () =>
    {
        Students.Clear();
        foreach (var student in await _store.GetStudentLookupsAsync()) Students.Add(student);
        Courses.Clear();
        foreach (var course in await _store.GetCourseLookupsAsync()) Courses.Add(course);
        Attendances.Clear();
        foreach (var item in await _store.GetAttendancesAsync()) Attendances.Add(item);
        RaisePropertyChanged(nameof(PresentRateText));
        Message = Students.Count == 0 || Courses.Count == 0 ? "Yoklama için öğrenci ve ders kaydı gerekir." : "";
    });

    private async Task SaveAsync() => await RunAsync(async () =>
    {
        var saved = await _store.SaveAttendanceAsync(_editingId, StudentId, CourseId, AttendanceDate, IsPresent);
        Replace(Attendances, saved, x => x.Id == saved.Id);
        SelectedAttendance = saved;
        Populate(saved);
        RaisePropertyChanged(nameof(PresentRateText));
        Message = "Yoklama kaydedildi.";
    });

    private async Task DeleteAsync()
    {
        if (SelectedAttendance is not { } attendance) return;
        if (MessageBox.Show($"{attendance.StudentName} yoklaması silinsin mi?", "Yoklamayı sil",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await RunAsync(async () =>
        {
            await _store.DeleteAttendanceAsync(attendance.Id);
            Attendances.Remove(attendance);
            Populate(null);
            RaisePropertyChanged(nameof(PresentRateText));
            Message = "Yoklama silindi.";
        });
    }

    private void Populate(AttendanceRow? attendance)
    {
        _editingId = attendance?.Id;
        StudentId = attendance?.StudentId ?? Guid.Empty;
        CourseId = attendance?.CourseId ?? Guid.Empty;
        AttendanceDate = attendance?.AttendanceDate ?? DateTime.Today;
        IsPresent = attendance?.IsPresent ?? true;
        RaisePropertyChanged(nameof(StudentId)); RaisePropertyChanged(nameof(CourseId));
        RaisePropertyChanged(nameof(AttendanceDate)); RaisePropertyChanged(nameof(IsPresent)); RaisePropertyChanged(nameof(SaveLabel));
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
