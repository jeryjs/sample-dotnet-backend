using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Patients;

/// <summary>
/// Request model for creating a patient.
/// </summary>
public class CreatePatientRequest
{
    /// <summary>
    /// Unique identifier for the patient.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Indicates whether the patient is billable.
    /// </summary>
    public bool? IsBillable { get; set; }

    /// <summary>
    /// Indicates whether the patient is PG billable.
    /// </summary>
    public bool? IsPgBillable { get; set; }

    /// <summary>
    /// Indicates whether the patient is eligible.
    /// </summary>
    public bool? IsEligible { get; set; }

    /// <summary>
    /// Indicates whether the patient is PG eligible.
    /// </summary>
    public bool? IsPgEligible { get; set; }

    /// <summary>
    /// Agency information for the patient.
    /// </summary>
    public required AgencyInfo AgencyInfo { get; set; }
}

/// <summary>
/// Response model for patient creation.
/// </summary>
public class CreatePatientResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PatientId { get; set; }
}

/// <summary>
/// Endpoint to create a new patient.
/// </summary>
public class CreatePatientEndpoint : Endpoint<CreatePatientRequest, CreatePatientResponse>
{
    private readonly ILogger<CreatePatientEndpoint> _logger;

    public CreatePatientEndpoint(ILogger<CreatePatientEndpoint> logger)
    {
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/patients");
        Policies("WriteAccess");
        Options(x => x
            .WithTags("Patients")
            .WithSummary("Create a new patient")
            .WithDescription("Creates a new patient in the system. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<CreatePatientResponse>(201, "application/json")
            .Produces<CreatePatientResponse>(400, "application/json")
            .Produces<CreatePatientResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(CreatePatientRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to create new patient");
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new CreatePatientResponse
            {
                Success = false,
                Message = "Patient creation not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Patient creation attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating patient");
            
            var response = new CreatePatientResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
