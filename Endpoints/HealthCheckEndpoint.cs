using FastEndpoints;

namespace BackendApi.Endpoints;

public class HealthCheckEndpoint : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Get("/health-check");
        Policies("DefaultAccess");
        Options(x => x.WithTags("Health"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var response = new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        };

        await SendAsync(response, cancellation: ct);
    }
}

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
}
