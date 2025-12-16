using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Stats;

/// <summary>
/// Response DTO for ancillary statistics.
/// </summary>
public class AncillaryStatsResponse
{
    /// <summary>
    /// Gets the total number of ancillaries.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the breakdown of ancillaries by entity type.
    /// </summary>
    public Dictionary<string, int> ByEntityType { get; init; } = new();

    /// <summary>
    /// Gets the breakdown of ancillaries by entity subtype (division).
    /// </summary>
    public Dictionary<string, int> ByDivision { get; init; } = new();

    /// <summary>
    /// Gets the breakdown of ancillaries by lifecycle stage.
    /// </summary>
    public Dictionary<string, int> ByLifecycleStage { get; init; } = new();
}

/// <summary>
/// Endpoint to retrieve ancillary statistics.
/// </summary>
public class GetAncillaryStatsEndpoint : EndpointWithoutRequest<AncillaryStatsResponse>
{
    private readonly IAncillaryUserRepository _repository;
    private readonly ILogger<GetAncillaryStatsEndpoint> _logger;

    public GetAncillaryStatsEndpoint(IAncillaryUserRepository repository, ILogger<GetAncillaryStatsEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/stats/ancillaries");
        AllowAnonymous();
        Options(x => x
            .WithTags("Stats")
            .WithSummary("Get ancillary statistics")
            .WithDescription("Retrieves aggregated statistics for ancillaries including total count and breakdown by entity type, division (subtype), and lifecycle stage")
            .Produces<AncillaryStatsResponse>(200, "application/json")
            .Produces(500, "application/json"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching ancillary statistics");
            
            var ancillaries = await _repository.GetAllAsync(ct);
            
            var stats = new AncillaryStatsResponse
            {
                TotalCount = ancillaries.Count,
                ByEntityType = ancillaries
                    .GroupBy(a => a.EntityType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ByDivision = ancillaries
                    .GroupBy(a => a.EntitySubtype ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count()),
                ByLifecycleStage = ancillaries
                    .GroupBy(a => a.LifecycleStage ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count())
            };
            
            _logger.LogInformation("Successfully retrieved ancillary statistics: Total={Total}, EntityTypes={Types}, Divisions={Divisions}, LifecycleStages={Stages}", 
                stats.TotalCount, stats.ByEntityType.Count, stats.ByDivision.Count, stats.ByLifecycleStage.Count);
            
            await SendAsync(stats, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching ancillary statistics");
            
            await SendAsync(new AncillaryStatsResponse(), 500, ct);
        }
    }
}
