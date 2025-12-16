using Serilog.Context;

namespace BackendApi.Middleware;

/// <summary>
/// Middleware that generates a unique request ID for each request.
/// The ID is added to response headers and logging context for traceability.
/// </summary>
public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string RequestIdHeaderName = "X-Request-Id";

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if request already has an ID (e.g., from load balancer)
        var requestId = context.Request.Headers[RequestIdHeaderName].FirstOrDefault();

        // Generate new ID if not present
        if (string.IsNullOrWhiteSpace(requestId))
        {
            requestId = Guid.NewGuid().ToString();
        }

        // Store in HttpContext items for access by other middleware
        context.Items["RequestId"] = requestId;

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(RequestIdHeaderName))
            {
                context.Response.Headers[RequestIdHeaderName] = requestId;
            }
            return Task.CompletedTask;
        });

        // Add to Serilog logging context
        using (LogContext.PushProperty("RequestId", requestId))
        {
            await _next(context);
        }
    }
}
