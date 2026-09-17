using System.Windows.Input;

namespace YAvuzeli.Client.ViewModels;

public class MainViewModel : BaseViewModel
{
    private object? _currentView;
    public object? CurrentView
    {
        get => _currentView;
        set { _currentView = value; RaisePropertyChanged(); }
    }

    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowStudentsCommand { get; }
    public ICommand ShowTeachersCommand { get; }
    public ICommand ShowCoursesCommand { get; }
    public ICommand ShowScheduleCommand { get; }
    public ICommand ShowAttendanceCommand { get; }
    public ICommand ShowPaymentsCommand { get; }
    public ICommand ShowReportsCommand { get; }
    public ICommand ShowSettingsCommand { get; }

    public MainViewModel()
    {
        ShowDashboardCommand = new RelayCommand(_ => CurrentView = new Views.DashboardView());
        ShowStudentsCommand = new RelayCommand(_ => CurrentView = new Views.StudentsView());
        ShowTeachersCommand = new RelayCommand(_ => CurrentView = new Views.TeachersView());
        ShowCoursesCommand = new RelayCommand(_ => CurrentView = new Views.CoursesView());
        ShowScheduleCommand = new RelayCommand(_ => CurrentView = new Views.ScheduleView());
        ShowAttendanceCommand = new RelayCommand(_ => CurrentView = new Views.AttendanceView());
        ShowPaymentsCommand = new RelayCommand(_ => CurrentView = new Views.PaymentsView());
        ShowReportsCommand = new RelayCommand(_ => CurrentView = new Views.ReportsView());
        ShowSettingsCommand = new RelayCommand(_ => CurrentView = new Views.SettingsView());

        // default view
        CurrentView = new Views.DashboardView();
    }
}
