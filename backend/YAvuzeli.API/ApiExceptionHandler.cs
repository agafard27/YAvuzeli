using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace YAvuzeli.API;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            KeyNotFoundException => (404, "Öğrenci bulunamadı."),
            ValidationException => (400, exception.Message),
            DbUpdateConcurrencyException => (409, "Kayıt başka bir işlemde değişti. Listeyi yenileyin."),
            DbUpdateException { InnerException: PostgresException { SqlState: "23503" } }
                or DbUpdateException { InnerException: SqliteException { SqliteExtendedErrorCode: 787 } }
                => (409, "Öğrencinin bağlı kayıtları var. Silmek yerine pasif yapabilirsiniz."),
            _ => (500, "İşlem tamamlanamadı. Lütfen tekrar deneyin.")
        };

        if (status == 500) logger.LogError(exception, "Student API request failed: {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = title,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, cancellationToken);
        return true;
    }
}
