using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AppleShop.Middleware;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception) { await HandleAsync(context, exception); }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var status = exception switch
        {
            AppException app => app.StatusCode,
            UnauthorizedAccessException => 401,
            KeyNotFoundException => 404,
            _ => 500
        };

        if (status >= 500) logger.LogError(exception, "Unhandled error on {Path}", context.Request.Path);
        else logger.LogWarning("Handled {Status} on {Path}: {Message}", status, context.Request.Path, exception.Message);

        if (context.Response.HasStarted) return;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = status switch { 400 => "Bad Request", 401 => "Unauthorized", 403 => "Forbidden", 404 => "Not Found", 409 => "Conflict", _ => "Internal Server Error" },
            Detail = status >= 500 ? "Сталася непередбачена помилка." : exception.Message,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        };

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, Options));
    }
}