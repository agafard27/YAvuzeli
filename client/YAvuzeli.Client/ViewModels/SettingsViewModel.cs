using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using YAvuzeli.Client.Services;

namespace YAvuzeli.Client.ViewModels;

public sealed class SettingsViewModel : BaseViewModel
{
    private readonly SchoolStore _store = new();
    private string _message = "";
    public string DatabasePath => _store.DatabasePath;
    public string Message { get => _message; private set { _message = value; RaisePropertyChanged(); } }
    public ICommand BackupCommand { get; }
    public ICommand RestoreCommand { get; }

    public SettingsViewModel()
    {
        BackupCommand = new RelayCommand(_ => Backup());
        RestoreCommand = new RelayCommand(_ => Restore());
    }

    private void Backup()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        if (!File.Exists(DatabasePath))
        {
            Message = "Henüz yedeklenecek veritabanı yok. Önce bir kayıt oluşturun.";
            return;
        }

        SqliteConnection.ClearAllPools();
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YAvuzeli-Yedekler");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"yavuzeli-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        File.Copy(DatabasePath, path, overwrite: false);
        Message = $"Yedek alındı: {path}";
    }

    private void Restore()
    {
        var dialog = new OpenFileDialog
        {
            Title = "YAvuzeli yedek dosyası seç",
            Filter = "SQLite yedeği (*.db)|*.db|Tüm dosyalar (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;
        if (MessageBox.Show("Seçilen yedek mevcut yerel verilerin üzerine yazılacak. Devam edilsin mi?",
            "Yedekten geri yükle", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        SqliteConnection.ClearAllPools();
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        if (File.Exists(DatabasePath))
        {
            var backup = DatabasePath + $".onceki-{DateTime.Now:yyyyMMdd-HHmmss}.bak";
            File.Copy(DatabasePath, backup, overwrite: false);
        }
        File.Copy(dialog.FileName, DatabasePath, overwrite: true);
        Message = "Yedek geri yüklendi. Açık ekranları yenileyin veya uygulamayı yeniden başlatın.";
    }
}
