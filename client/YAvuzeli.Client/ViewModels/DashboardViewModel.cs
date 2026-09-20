using System.Globalization;
using YAvuzeli.Client.Services;

namespace YAvuzeli.Client.ViewModels;

public sealed class DashboardViewModel : BaseViewModel
{
    private readonly SchoolStore _store = new();
    public string StudentsText { get; private set; } = "0";
    public string TeachersText { get; private set; } = "0";
    public string CoursesText { get; private set; } = "0";
    public string RevenueText { get; private set; } = "₺0";
    public string AttendanceText { get; private set; } = "%0";
    public string Message { get; private set; } = "Veriler yükleniyor...";

    public async Task LoadAsync()
    {
        var stats = await _store.GetDashboardStatsAsync();
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        StudentsText = stats.Students.ToString("N0", tr);
        TeachersText = stats.Teachers.ToString("N0", tr);
        CoursesText = stats.Courses.ToString("N0", tr);
        RevenueText = stats.Revenue.ToString("C0", tr);
        AttendanceText = stats.AttendanceCount == 0 ? "%0" : $"%{stats.PresentCount * 100 / stats.AttendanceCount}";
        Message = "Bugünkü operasyon özeti hazır.";
        RaisePropertyChanged(nameof(StudentsText)); RaisePropertyChanged(nameof(TeachersText));
        RaisePropertyChanged(nameof(CoursesText)); RaisePropertyChanged(nameof(RevenueText));
        RaisePropertyChanged(nameof(AttendanceText)); RaisePropertyChanged(nameof(Message));
    }
}
