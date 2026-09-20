using System.Windows.Input;

namespace YAvuzeli.Client.ViewModels;

public class MainViewModel : BaseViewModel
{
    // Keep the editor state when navigating to another module and back.
    private readonly Lazy<Views.StudentsView> _students = new(() => new Views.StudentsView());
    public bool CanClose() => !_students.IsValueCreated ||
        (_students.Value.Model.IsReady && _students.Value.Model.ConfirmDiscard());
    private object? _currentView;
    private string _pageTitle = "";
    private string _pageSubtitle = "";

    public object? CurrentView
    {
        get => _currentView;
        set { _currentView = value; RaisePropertyChanged(); }
    }

    public string PageTitle
    {
        get => _pageTitle;
        private set { _pageTitle = value; RaisePropertyChanged(); }
    }

    public string PageSubtitle
    {
        get => _pageSubtitle;
        private set { _pageSubtitle = value; RaisePropertyChanged(); }
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
        ShowDashboardCommand = new RelayCommand(_ => Show(new Views.DashboardView(), "Dashboard", "Günlük özet ve hızlı erişim."));
        ShowStudentsCommand = new RelayCommand(_ => Show(_students.Value, "Öğrenciler", "Kayıt, iletişim ve durum bilgilerini yönetin."));
        ShowTeachersCommand = new RelayCommand(_ => Show(new Views.TeachersView(), "Öğretmenler", "Öğretmen kayıtları ve görev bilgileri."));
        ShowCoursesCommand = new RelayCommand(_ => Show(new Views.CoursesView(), "Dersler", "Ders ve sınıf tanımları."));
        ShowScheduleCommand = new RelayCommand(_ => Show(new Views.ScheduleView(), "Program", "Haftalık ders planı."));
        ShowAttendanceCommand = new RelayCommand(_ => Show(new Views.AttendanceView(), "Yoklama", "Ders yoklama takibi."));
        ShowPaymentsCommand = new RelayCommand(_ => Show(new Views.PaymentsView(), "Ödemeler", "Tahsilat ve ödeme kayıtları."));
        ShowReportsCommand = new RelayCommand(_ => Show(new Views.ReportsView(), "Raporlar", "Dönem ve finans raporları."));
        ShowSettingsCommand = new RelayCommand(_ => Show(new Views.SettingsView(), "Ayarlar", "Uygulama ve yedekleme ayarları."));

        // default view
        Show(_students.Value, "Öğrenciler", "Kayıt, iletişim ve durum bilgilerini yönetin.");
    }

    private void Show(object view, string title, string subtitle)
    {
        CurrentView = view;
        PageTitle = title;
        PageSubtitle = subtitle;
    }
}
