using System.Net;
using System.Text.Json;

namespace Hospital.WebApi.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing request {Method} {Path}", context.Request?.Method, context.Request?.Path);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning( "Response has already started, exception middleware will not write the response body." );
                throw;
            }

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError; // Just 500

            var response = new
            {
                error = "InternalServerError",
                // Detailed message is only available in development
                message = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred."
            };

            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json).ConfigureAwait(false);
        }
    }
}