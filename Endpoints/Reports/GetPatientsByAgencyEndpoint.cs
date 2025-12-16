using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Reports;

/// <summary>
/// DTO for a group of patients by agency.
/// </summary>
public class PatientsByAgencyGroup
{
    /// <summary>
    /// Gets the agency name.
    /// </summary>
    public string AgencyName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the count of patients in this agency.
    /// </summary>
    public int PatientCount { get; init; }

    /// <summary>
    /// Gets the list of patients in this agency.
    /// </summary>
    public List<PatientSummary> Patients { get; init; } = new();
}

/// <summary>
/// DTO for patient summary information.
/// </summary>
public class PatientSummary
{
    /// <summary>
    /// Gets the patient ID.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the patient WAV ID.
    /// </summary>
    public string? PatientWAVId { get; init; }

    /// <summary>
    /// Gets the patient's full name.
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the patient's status.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Gets the patient's date of birth.
    /// </summary>
    public string? DateOfBirth { get; init; }
}

/// <summary>
/// Response DTO for patients grouped by agency.
/// </summary>
public class PatientsByAgencyResponse
{
    /// <summary>
    /// Gets the list of patient groups by agency.
    /// </summary>
    public List<PatientsByAgencyGroup> Groups { get; init; } = new();

    /// <summary>
    /// Gets the total number of patients across all agencies.
    /// </summary>
    public int TotalPatients { get; init; }

    /// <summary>
    /// Gets the total number of agencies.
    /// </summary>
    public int TotalAgencies { get; init; }
}

/// <summary>
/// Endpoint to retrieve patients grouped by agency name.
/// </summary>
public class GetPatientsByAgencyEndpoint : EndpointWithoutRequest<PatientsByAgencyResponse>
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<GetPatientsByAgencyEndpoint> _logger;

    public GetPatientsByAgencyEndpoint(IPatientRepository repository, ILogger<GetPatientsByAgencyEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/reports/patients-by-agency");
        AllowAnonymous();
        Options(x => x
            .WithTags("Reports")
            .WithSummary("Get patients grouped by agency")
            .WithDescription("Retrieves all patients grouped by their agency name with summary information")
            .Produces<PatientsByAgencyResponse>(200, "application/json")
            .Produces(500, "application/json"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching patients grouped by agency");
            
            var patients = await _repository.GetAllAsync(ct);
            
            // Group patients by care management type as agency identifier
            var groups = patients
                .GroupBy(p => p.AgencyInfo.CareManagement?.FirstOrDefault()?.CareManagementType ?? "Unknown Agency")
                .Select(g => new PatientsByAgencyGroup
                {
                    AgencyName = g.Key,
                    PatientCount = g.Count(),
                    Patients = g.Select(p => new PatientSummary
                    {
                        Id = p.Id,
                        PatientWAVId = p.AgencyInfo.PatientWAVId,
                        FullName = $"{p.AgencyInfo.PatientFName} {p.AgencyInfo.PatientLName}".Trim(),
                        Status = p.AgencyInfo.PatientStatus,
                        DateOfBirth = p.AgencyInfo.Dob
                    }).ToList()
                })
                .OrderByDescending(g => g.PatientCount)
                .ToList();
            
            var response = new PatientsByAgencyResponse
            {
                Groups = groups,
                TotalPatients = patients.Count,
                TotalAgencies = groups.Count
            };
            
            _logger.LogInformation("Successfully retrieved patients by agency: {AgencyCount} agencies, {PatientCount} patients", 
                response.TotalAgencies, response.TotalPatients);
            
            await SendAsync(response, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching patients by agency");
            
            await SendAsync(new PatientsByAgencyResponse(), 500, ct);
        }
    }
}
