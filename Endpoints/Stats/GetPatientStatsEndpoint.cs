using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Stats;

/// <summary>
/// Response DTO for patient statistics.
/// </summary>
public class PatientStatsResponse
{
    /// <summary>
    /// Gets the total number of patients.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the number of active patients.
    /// </summary>
    public int ActiveCount { get; init; }

    /// <summary>
    /// Gets the number of inactive patients.
    /// </summary>
    public int InactiveCount { get; init; }

    /// <summary>
    /// Gets the breakdown of patients by state.
    /// </summary>
    public Dictionary<string, int> ByState { get; init; } = new();
}

/// <summary>
/// Endpoint to retrieve patient statistics.
/// </summary>
public class GetPatientStatsEndpoint : EndpointWithoutRequest<PatientStatsResponse>
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<GetPatientStatsEndpoint> _logger;

    public GetPatientStatsEndpoint(IPatientRepository repository, ILogger<GetPatientStatsEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/stats/patients");
        AllowAnonymous();
        Options(x => x
            .WithTags("Stats")
            .WithSummary("Get patient statistics")
            .WithDescription("Retrieves aggregated statistics for patients including total count, active/inactive split, and breakdown by state")
            .Produces<PatientStatsResponse>(200, "application/json")
            .Produces(500, "application/json"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching patient statistics");
            
            var patients = await _repository.GetAllAsync(ct);
            
            var stats = new PatientStatsResponse
            {
                TotalCount = patients.Count,
                ActiveCount = patients.Count(p => 
                    p.AgencyInfo.PatientStatus?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true),
                InactiveCount = patients.Count(p => 
                    p.AgencyInfo.PatientStatus?.Equals("Inactive", StringComparison.OrdinalIgnoreCase) == true),
                ByState = patients
                    .Where(p => !string.IsNullOrWhiteSpace(p.AgencyInfo.CareManagement?.FirstOrDefault()?.CareManagementType))
                    .GroupBy(p => p.AgencyInfo.CareManagement?.FirstOrDefault()?.CareManagementType ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count())
            };
            
            _logger.LogInformation("Successfully retrieved patient statistics: Total={Total}, Active={Active}, Inactive={Inactive}", 
                stats.TotalCount, stats.ActiveCount, stats.InactiveCount);
            
            await SendAsync(stats, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching patient statistics");
            
            await SendAsync(new PatientStatsResponse(), 500, ct);
        }
    }
}
