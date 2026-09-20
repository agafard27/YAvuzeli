using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using YAvuzeli.Client.Services;

namespace YAvuzeli.Client.ViewModels;

public sealed record DayOption(DayOfWeek Id, string Name);

public sealed class ProgramViewModel : BaseViewModel
{
    private readonly SchoolStore _store = new();
    private Guid? _editingEnrollmentId;
    private Guid? _editingScheduleId;
    private EnrollmentRow? _selectedEnrollment;
    private ScheduleRow? _selectedSchedule;
    private string _message = "";
    private bool _busy;

    public ObservableCollection<EnrollmentRow> Enrollments { get; } = [];
    public ObservableCollection<ScheduleRow> Schedule { get; } = [];
    public ObservableCollection<LookupRow> Students { get; } = [];
    public ObservableCollection<LookupRow> Courses { get; } = [];
    public ObservableCollection<DayOption> Days { get; } =
    [
        new(DayOfWeek.Monday, "Pazartesi"),
        new(DayOfWeek.Tuesday, "Salı"),
        new(DayOfWeek.Wednesday, "Çarşamba"),
        new(DayOfWeek.Thursday, "Perşembe"),
        new(DayOfWeek.Friday, "Cuma"),
        new(DayOfWeek.Saturday, "Cumartesi"),
        new(DayOfWeek.Sunday, "Pazar")
    ];

    public Guid EnrollmentStudentId { get; set; }
    public Guid EnrollmentCourseId { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.Today;
    public string Fee { get; set; } = "";
    public bool IsPaid { get; set; }
    public Guid ScheduleCourseId { get; set; }
    public DayOfWeek ScheduleDay { get; set; } = DayOfWeek.Monday;
    public string StartTime { get; set; } = "09:00";
    public string EndTime { get; set; } = "10:00";
    public string Room { get; set; } = "";
    public string Message { get => _message; set { _message = value; RaisePropertyChanged(); } }
    public bool IsReady => !_busy;
    public string EnrollmentSaveLabel => _editingEnrollmentId.HasValue ? "Kaydı güncelle" : "Öğrenciyi derse kaydet";
    public string ScheduleSaveLabel => _editingScheduleId.HasValue ? "Programı güncelle" : "Programı kaydet";
    public EnrollmentRow? SelectedEnrollment { get => _selectedEnrollment; set { _selectedEnrollment = value; RaisePropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
    public ScheduleRow? SelectedSchedule { get => _selectedSchedule; set { _selectedSchedule = value; RaisePropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
    public ICommand RefreshCommand { get; }
    public ICommand SaveEnrollmentCommand { get; }
    public ICommand EditEnrollmentCommand { get; }
    public ICommand NewEnrollmentCommand { get; }
    public ICommand DeleteEnrollmentCommand { get; }
    public ICommand SaveScheduleCommand { get; }
    public ICommand EditScheduleCommand { get; }
    public ICommand NewScheduleCommand { get; }
    public ICommand DeleteScheduleCommand { get; }

    public ProgramViewModel()
    {
        RefreshCommand = Async(LoadAsync);
        SaveEnrollmentCommand = Async(SaveEnrollmentAsync);
        EditEnrollmentCommand = new RelayCommand(_ => PopulateEnrollment(SelectedEnrollment), _ => SelectedEnrollment is not null);
        NewEnrollmentCommand = new RelayCommand(_ => PopulateEnrollment(null));
        DeleteEnrollmentCommand = Async(DeleteEnrollmentAsync, () => SelectedEnrollment is not null);
        SaveScheduleCommand = Async(SaveScheduleAsync);
        EditScheduleCommand = new RelayCommand(_ => PopulateSchedule(SelectedSchedule), _ => SelectedSchedule is not null);
        NewScheduleCommand = new RelayCommand(_ => PopulateSchedule(null));
        DeleteScheduleCommand = Async(DeleteScheduleAsync, () => SelectedSchedule is not null);
    }

    public async Task LoadAsync() => await RunAsync(async () =>
    {
        Students.Clear();
        foreach (var student in await _store.GetStudentLookupsAsync()) Students.Add(student);
        Courses.Clear();
        foreach (var course in await _store.GetCourseLookupsAsync()) Courses.Add(course);
        await ReloadEnrollmentsAsync();
        await ReloadScheduleAsync();
        Message = Students.Count == 0 || Courses.Count == 0
            ? "Program için önce öğrenci ve ders kayıtlarını oluşturun."
            : "Program verileri güncel.";
    });

    private async Task SaveEnrollmentAsync() => await RunAsync(async () =>
    {
        if (!decimal.TryParse(Fee, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out var fee) &&
            !decimal.TryParse(Fee, NumberStyles.Number, CultureInfo.InvariantCulture, out fee))
            throw new InvalidOperationException("Geçerli bir ücret girin.");
        await _store.SaveEnrollmentAsync(_editingEnrollmentId, EnrollmentStudentId, EnrollmentCourseId, EnrollmentDate, fee, IsPaid);
        await ReloadEnrollmentsAsync();
        PopulateEnrollment(null);
        Message = "Ders kaydı kaydedildi.";
    });

    private async Task SaveScheduleAsync() => await RunAsync(async () =>
    {
        if (!TimeSpan.TryParse(StartTime, CultureInfo.GetCultureInfo("tr-TR"), out var start))
            throw new InvalidOperationException("Başlangıç saatini 09:00 formatında girin.");
        if (!TimeSpan.TryParse(EndTime, CultureInfo.GetCultureInfo("tr-TR"), out var end))
            throw new InvalidOperationException("Bitiş saatini 10:00 formatında girin.");
        await _store.SaveScheduleAsync(_editingScheduleId, ScheduleCourseId, ScheduleDay, start, end, Room);
        await ReloadScheduleAsync();
        PopulateSchedule(null);
        Message = "Haftalık program kaydedildi.";
    });

    private async Task DeleteEnrollmentAsync()
    {
        if (SelectedEnrollment is not { } enrollment) return;
        if (MessageBox.Show($"{enrollment.StudentName} ders kaydı silinsin mi?", "Ders kaydını sil",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await RunAsync(async () =>
        {
            await _store.DeleteEnrollmentAsync(enrollment.Id);
            await ReloadEnrollmentsAsync();
            PopulateEnrollment(null);
            Message = "Ders kaydı silindi.";
        });
    }

    private async Task DeleteScheduleAsync()
    {
        if (SelectedSchedule is not { } item) return;
        if (MessageBox.Show($"{item.DayName} {item.CourseTitle} programı silinsin mi?", "Programı sil",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await RunAsync(async () =>
        {
            await _store.DeleteScheduleAsync(item.Id);
            await ReloadScheduleAsync();
            PopulateSchedule(null);
            Message = "Program kaydı silindi.";
        });
    }

    private async Task ReloadEnrollmentsAsync()
    {
        Enrollments.Clear();
        foreach (var item in await _store.GetEnrollmentsAsync()) Enrollments.Add(item);
    }

    private async Task ReloadScheduleAsync()
    {
        Schedule.Clear();
        foreach (var item in await _store.GetScheduleAsync()) Schedule.Add(item);
    }

    private void PopulateEnrollment(EnrollmentRow? row)
    {
        _editingEnrollmentId = row?.Id;
        EnrollmentStudentId = row?.StudentId ?? Guid.Empty;
        EnrollmentCourseId = row?.CourseId ?? Guid.Empty;
        EnrollmentDate = row?.EnrollmentDate ?? DateTime.Today;
        Fee = row?.Fee.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR")) ?? "";
        IsPaid = row?.IsPaid ?? false;
        RaisePropertyChanged(nameof(EnrollmentStudentId)); RaisePropertyChanged(nameof(EnrollmentCourseId));
        RaisePropertyChanged(nameof(EnrollmentDate)); RaisePropertyChanged(nameof(Fee));
        RaisePropertyChanged(nameof(IsPaid)); RaisePropertyChanged(nameof(EnrollmentSaveLabel));
    }

    private void PopulateSchedule(ScheduleRow? row)
    {
        _editingScheduleId = row?.Id;
        ScheduleCourseId = row?.CourseId ?? Guid.Empty;
        ScheduleDay = row?.DayOfWeek ?? DayOfWeek.Monday;
        StartTime = row?.StartTime.ToString(@"hh\:mm") ?? "09:00";
        EndTime = row?.EndTime.ToString(@"hh\:mm") ?? "10:00";
        Room = row?.Room ?? "";
        RaisePropertyChanged(nameof(ScheduleCourseId)); RaisePropertyChanged(nameof(ScheduleDay));
        RaisePropertyChanged(nameof(StartTime)); RaisePropertyChanged(nameof(EndTime));
        RaisePropertyChanged(nameof(Room)); RaisePropertyChanged(nameof(ScheduleSaveLabel));
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
}
