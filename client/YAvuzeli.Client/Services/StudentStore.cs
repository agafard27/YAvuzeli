using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using YAvuzeli.Application.Services;
using YAvuzeli.Infrastructure.Data;
using YAvuzeli.Infrastructure.Repositories;
using YAvuzeli.Shared.Students;

namespace YAvuzeli.Client.Services;

// Each operation owns its context; the UI never shares a DbContext across threads.
public sealed class StudentStore
{
    public string DatabasePath { get; }

    public StudentStore(string? dataDirectory = null)
    {
        var directory = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YAvuzeli");
        DatabasePath = Path.Combine(directory, "yavuzeli.db");
    }

    public Task<List<StudentDto>> GetAllAsync() => WithServiceAsync(async service =>
        (await service.GetAllAsync()).ToList());

    public Task<StudentDto> SaveAsync(Guid? id, StudentInput input) => WithServiceAsync(service =>
        id is { } existing ? service.UpdateAsync(existing, input) : service.CreateAsync(input));

    public async Task DeleteAsync(Guid id) => await WithServiceAsync(async service =>
    {
        await service.DeleteAsync(id);
        return true;
    });

    private Task<T> WithServiceAsync<T>(Func<StudentService, Task<T>> action) => Task.Run(async () =>
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        var connection = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath, ForeignKeys = true, DefaultTimeout = 15
        }.ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return await action(new StudentService(new StudentRepository(db)));
    });
}
