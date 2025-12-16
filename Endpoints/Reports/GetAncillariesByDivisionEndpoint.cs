using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Reports;

/// <summary>
/// DTO for a group of ancillaries by division.
/// </summary>
public class AncillariesByDivisionGroup
{
    /// <summary>
    /// Gets the division name (entity subtype).
    /// </summary>
    public string Division { get; init; } = string.Empty;

    /// <summary>
    /// Gets the count of ancillaries in this division.
    /// </summary>
    public int AncillaryCount { get; init; }

    /// <summary>
    /// Gets the list of ancillaries in this division.
    /// </summary>
    public List<AncillarySummary> Ancillaries { get; init; } = new();
}

/// <summary>
/// DTO for ancillary summary information.
/// </summary>
public class AncillarySummary
{
    /// <summary>
    /// Gets the ancillary ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the entity WAV ID.
    /// </summary>
    public string EntityWavId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the ancillary name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the entity type.
    /// </summary>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the lifecycle stage.
    /// </summary>
    public string? LifecycleStage { get; init; }

    /// <summary>
    /// Gets the state location.
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// Gets the city location.
    /// </summary>
    public string? City { get; init; }
}

/// <summary>
/// Response DTO for ancillaries grouped by division.
/// </summary>
public class AncillariesByDivisionResponse
{
    /// <summary>
    /// Gets the list of ancillary groups by division.
    /// </summary>
    public List<AncillariesByDivisionGroup> Groups { get; init; } = new();

    /// <summary>
    /// Gets the total number of ancillaries across all divisions.
    /// </summary>
    public int TotalAncillaries { get; init; }

    /// <summary>
    /// Gets the total number of divisions.
    /// </summary>
    public int TotalDivisions { get; init; }
}

/// <summary>
/// Endpoint to retrieve ancillaries grouped by division (entity subtype).
/// </summary>
public class GetAncillariesByDivisionEndpoint : EndpointWithoutRequest<AncillariesByDivisionResponse>
{
    private readonly IAncillaryUserRepository _repository;
    private readonly ILogger<GetAncillariesByDivisionEndpoint> _logger;

    public GetAncillariesByDivisionEndpoint(IAncillaryUserRepository repository, ILogger<GetAncillariesByDivisionEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/reports/ancillaries-by-division");
        AllowAnonymous();
        Options(x => x
            .WithTags("Reports")
            .WithSummary("Get ancillaries grouped by division")
            .WithDescription("Retrieves all ancillaries grouped by their division (entity subtype) with summary information")
            .Produces<AncillariesByDivisionResponse>(200, "application/json")
            .Produces(500, "application/json"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching ancillaries grouped by division");
            
            var ancillaries = await _repository.GetAllAsync(ct);
            
            var groups = ancillaries
                .GroupBy(a => a.EntitySubtype ?? "Unknown Division")
                .Select(g => new AncillariesByDivisionGroup
                {
                    Division = g.Key,
                    AncillaryCount = g.Count(),
                    Ancillaries = g.Select(a => new AncillarySummary
                    {
                        Id = a.Id,
                        EntityWavId = a.EntityWavId,
                        Name = a.Name,
                        EntityType = a.EntityType,
                        LifecycleStage = a.LifecycleStage,
                        State = a.State,
                        City = a.City
                    }).ToList()
                })
                .OrderByDescending(g => g.AncillaryCount)
                .ToList();
            
            var response = new AncillariesByDivisionResponse
            {
                Groups = groups,
                TotalAncillaries = ancillaries.Count,
                TotalDivisions = groups.Count
            };
            
            _logger.LogInformation("Successfully retrieved ancillaries by division: {DivisionCount} divisions, {AncillaryCount} ancillaries", 
                response.TotalDivisions, response.TotalAncillaries);
            
            await SendAsync(response, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching ancillaries by division");
            
            await SendAsync(new AncillariesByDivisionResponse(), 500, ct);
        }
    }
}
