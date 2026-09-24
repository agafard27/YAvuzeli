using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using YAvuzeli.Infrastructure.Data;
using YAvuzeli.Application.Repositories;
using YAvuzeli.Application.Services;
using YAvuzeli.Infrastructure.Repositories;
using YAvuzeli.API;

var builder = WebApplication.CreateBuilder(args);

var provider = builder.Configuration["Database:Provider"] ?? "PostgreSQL";
var useSqlite = provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);
if (!useSqlite && !provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Database:Provider must be Sqlite or PostgreSQL.");
var conn = builder.Configuration.GetConnectionString(useSqlite ? "SqliteConnection" : "DefaultConnection")
    ?? throw new InvalidOperationException("Veritabanı bağlantı ayarı bulunamadı.");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useSqlite) options.UseSqlite(conn);
    else options.UseNpgsql(conn);
});

builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentService, StudentService>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
// Only the disposable/local SQLite setup is auto-created. PostgreSQL uses migrations.
if (useSqlite && app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/", () => Results.Ok(new { status = "YAvuzeli API is running" }));

app.Run();
