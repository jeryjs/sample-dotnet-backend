using System.Diagnostics;

namespace BackendApi.Middleware;

/// <summary>
/// Middleware that measures response time and adds X-Response-Time-Ms header.
/// Also logs warnings for slow requests exceeding 1000ms.
/// </summary>
public class ResponseTimeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseTimeMiddleware> _logger;
    private const int SlowRequestThresholdMs = 1000;

    public ResponseTimeMiddleware(RequestDelegate next, ILogger<ResponseTimeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // Add event handler to capture response time when response starts
        context.Response.OnStarting(() =>
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            
            // Add response time header
            context.Response.Headers["X-Response-Time-Ms"] = elapsedMs.ToString();
            
            return Task.CompletedTask;
        });

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Log slow requests
            if (elapsedMs >= SlowRequestThresholdMs)
            {
                var requestId = context.Items["RequestId"]?.ToString() ?? context.TraceIdentifier;
                
                _logger.LogWarning(
                    "Slow request detected. RequestId: {RequestId}, Method: {Method}, Path: {Path}, Duration: {ElapsedMs}ms, StatusCode: {StatusCode}",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    elapsedMs,
                    context.Response.StatusCode
                );
            }
        }
    }
}
