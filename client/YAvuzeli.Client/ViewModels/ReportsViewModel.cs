using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Input;
using YAvuzeli.Client.Services;

namespace YAvuzeli.Client.ViewModels;

public sealed record ReportLine(string Title, string Value, string Detail);

public sealed class ReportsViewModel : BaseViewModel
{
    private readonly SchoolStore _store = new();
    public ObservableCollection<ReportLine> Lines { get; } = [];
    public string Message { get; private set; } = "Rapor hazırlanıyor...";
    public ICommand ExportCsvCommand { get; }

    public ReportsViewModel()
    {
        ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync, () => true, e => SetMessage(e.Message));
    }

    public async Task LoadAsync()
    {
        var stats = await _store.GetDashboardStatsAsync();
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        Lines.Clear();
        Lines.Add(new ReportLine("Öğrenci", stats.Students.ToString("N0", tr), "Sistemde kayıtlı toplam öğrenci."));
        Lines.Add(new ReportLine("Aktif öğretmen", stats.Teachers.ToString("N0", tr), "Derslere atanabilecek öğretmenler."));
        Lines.Add(new ReportLine("Aktif ders", stats.Courses.ToString("N0", tr), "Program ve yoklamada kullanılacak dersler."));
        Lines.Add(new ReportLine("Toplam tahsilat", stats.Revenue.ToString("C0", tr), "Kaydedilen ödeme toplamı."));
        Lines.Add(new ReportLine("Yoklama başarı", stats.AttendanceCount == 0 ? "%0" : $"%{stats.PresentCount * 100 / stats.AttendanceCount}", "Girilmiş yoklamalarda mevcut oranı."));
        Message = "Rapor verileri güncel.";
        RaisePropertyChanged(nameof(Message));
    }

    private async Task ExportCsvAsync()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YAvuzeli-Raporlar");
        Directory.CreateDirectory(directory);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var path = Path.Combine(directory, $"rapor-{stamp}.csv");
        var payments = await _store.GetPaymentsAsync();
        var attendances = await _store.GetAttendancesAsync();
        var enrollments = await _store.GetEnrollmentsAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Bolum;Baslik;Deger;Detay");
        foreach (var line in Lines) csv.AppendLine($"Ozet;{Csv(line.Title)};{Csv(line.Value)};{Csv(line.Detail)}");
        foreach (var payment in payments)
            csv.AppendLine($"Odeme;{Csv(payment.StudentName)};{payment.Amount.ToString(CultureInfo.InvariantCulture)};{payment.PaymentDate:yyyy-MM-dd} {Csv(payment.PaymentMethod)} {Csv(payment.Note ?? "")}");
        foreach (var attendance in attendances)
            csv.AppendLine($"Yoklama;{Csv(attendance.StudentName)};{Csv(attendance.CourseTitle)};{attendance.AttendanceDate:yyyy-MM-dd} {(attendance.IsPresent ? "Geldi" : "Gelmedi")}");
        foreach (var enrollment in enrollments)
            csv.AppendLine($"Kayit;{Csv(enrollment.StudentName)};{Csv(enrollment.CourseTitle)};{enrollment.EnrollmentDate:yyyy-MM-dd} {enrollment.Fee.ToString(CultureInfo.InvariantCulture)}");
        await File.WriteAllTextAsync(path, csv.ToString(), new UTF8Encoding(true));
        SetMessage($"CSV rapor hazır: {path}");
    }

    private void SetMessage(string message)
    {
        Message = message;
        RaisePropertyChanged(nameof(Message));
    }

    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
