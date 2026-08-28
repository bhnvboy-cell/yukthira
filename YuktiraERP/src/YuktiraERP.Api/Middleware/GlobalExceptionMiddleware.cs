using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace YuktiraERP.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Unhandled exception occurred after response started");
                throw;
            }

            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            var status = ex switch
            {
                System.Collections.Generic.KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            var exposeDetail = _env.IsDevelopment() || status != StatusCodes.Status500InternalServerError;
            var problem = new
            {
                type = status == StatusCodes.Status404NotFound ? "https://tools.ietf.org/html/rfc7231#section-6.5.4" : "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = status == StatusCodes.Status404NotFound ? "Not Found" : status == StatusCodes.Status401Unauthorized ? "Unauthorized" : "Internal Server Error",
                status,
                detail = exposeDetail ? ex.Message : "An unexpected error occurred. Please try again later.",
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
