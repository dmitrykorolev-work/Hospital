using System.Diagnostics;

namespace Hospital.WebApi.Middlewares;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var request = context.Request;

        _logger.LogInformation( "Incoming request {Method} {Path}{QueryString}", request.Method, request.Path, request.QueryString);

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            var statusCode = context.Response?.StatusCode;
            _logger.LogInformation( "Request {Method} {Path} completed with {StatusCode} in {ElapsedMilliseconds}ms",
                request.Method, request.Path, statusCode, sw.ElapsedMilliseconds);
        }
    }
}