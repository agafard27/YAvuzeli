using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using YAvuzeli.Client.Services;

namespace YAvuzeli.Client.ViewModels;

public sealed class CoursesViewModel : BaseViewModel
{
    private readonly SchoolStore _store = new();
    private Guid? _editingId;
    private CourseRow? _selected;
    private string _message = "";
    private bool _busy;
    public ObservableCollection<CourseRow> Courses { get; } = [];
    public ObservableCollection<LookupRow> Teachers { get; } = [];
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid? TeacherId { get; set; }
    public bool IsActive { get; set; } = true;
    public string Message { get => _message; set { _message = value; RaisePropertyChanged(); } }
    public bool IsReady => !_busy;
    public CourseRow? SelectedCourse { get => _selected; set { _selected = value; RaisePropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
    public string SaveLabel => _editingId.HasValue ? "Değişiklikleri kaydet" : "Dersi kaydet";
    public ICommand RefreshCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand DeleteCommand { get; }

    public CoursesViewModel()
    {
        RefreshCommand = Async(LoadAsync);
        SaveCommand = Async(SaveAsync);
        EditCommand = new RelayCommand(_ => Populate(SelectedCourse), _ => SelectedCourse is not null);
        NewCommand = new RelayCommand(_ => Populate(null));
        DeleteCommand = Async(DeleteAsync, () => SelectedCourse is not null);
    }

    public async Task LoadAsync() => await RunAsync(async () =>
    {
        Teachers.Clear();
        foreach (var teacher in await _store.GetTeacherLookupsAsync()) Teachers.Add(teacher);
        Courses.Clear();
        foreach (var item in await _store.GetCoursesAsync()) Courses.Add(item);
        Message = Courses.Count == 0 ? "Henüz ders kaydı yok." : "";
    });

    private async Task SaveAsync() => await RunAsync(async () =>
    {
        var saved = await _store.SaveCourseAsync(_editingId, Title, Description, TeacherId, IsActive);
        Replace(Courses, saved, x => x.Id == saved.Id);
        SelectedCourse = saved;
        Populate(saved);
        Message = "Ders kaydedildi.";
    });

    private async Task DeleteAsync()
    {
        if (SelectedCourse is not { } course) return;
        if (MessageBox.Show($"{course.Title} silinsin mi?", "Dersi sil",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await RunAsync(async () =>
        {
            await _store.DeleteCourseAsync(course.Id);
            Courses.Remove(course);
            Populate(null);
            Message = "Ders silindi.";
        });
    }

    private void Populate(CourseRow? course)
    {
        _editingId = course?.Id;
        Title = course?.Title ?? "";
        Description = course?.Description ?? "";
        TeacherId = course?.TeacherId;
        IsActive = course?.IsActive ?? true;
        RaisePropertyChanged(nameof(Title)); RaisePropertyChanged(nameof(Description));
        RaisePropertyChanged(nameof(TeacherId)); RaisePropertyChanged(nameof(IsActive)); RaisePropertyChanged(nameof(SaveLabel));
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
