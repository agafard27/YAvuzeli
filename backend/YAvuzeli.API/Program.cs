using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using YAvuzeli.Infrastructure.Data;
using YAvuzeli.Application.Repositories;
using YAvuzeli.Application.Services;
using YAvuzeli.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure DbContext using connection string from appsettings
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
	options.UseNpgsql(conn);
});

builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentService, StudentService>();

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { status = "YAvuzeli API is running" }));

app.Run();
