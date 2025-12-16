using FastEndpoints;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Patients;

/// <summary>
/// Request model for partially updating a patient.
/// </summary>
public class PatchPatientRequest
{
    /// <summary>
    /// The unique identifier of the patient to update.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the patient is billable (optional).
    /// </summary>
    public bool? IsBillable { get; set; }

    /// <summary>
    /// Indicates whether the patient is PG billable (optional).
    /// </summary>
    public bool? IsPgBillable { get; set; }

    /// <summary>
    /// Indicates whether the patient is eligible (optional).
    /// </summary>
    public bool? IsEligible { get; set; }

    /// <summary>
    /// Indicates whether the patient is PG eligible (optional).
    /// </summary>
    public bool? IsPgEligible { get; set; }

    /// <summary>
    /// Patient status to update (optional).
    /// </summary>
    public string? PatientStatus { get; set; }
}

/// <summary>
/// Response model for patient patch.
/// </summary>
public class PatchPatientResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to partially update an existing patient.
/// </summary>
public class PatchPatientEndpoint : Endpoint<PatchPatientRequest, PatchPatientResponse>
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<PatchPatientEndpoint> _logger;

    public PatchPatientEndpoint(IPatientRepository repository, ILogger<PatchPatientEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Patch("/patients/{id}");
        AllowAnonymous();
        Options(x => x
            .WithTags("Patients")
            .WithSummary("Partially update a patient")
            .WithDescription("Partially updates an existing patient with only the provided fields. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<PatchPatientResponse>(200, "application/json")
            .Produces<PatchPatientResponse>(404, "application/json")
            .Produces<PatchPatientResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(PatchPatientRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to patch patient with ID: {Id}", req.Id);
            
            // Check if patient exists
            var existingPatient = await _repository.GetByIdAsync(req.Id, ct);
            
            if (existingPatient == null)
            {
                _logger.LogWarning("Patient with ID {Id} not found", req.Id);
                
                var notFoundResponse = new PatchPatientResponse
                {
                    Success = false,
                    Message = $"Patient with ID {req.Id} not found"
                };
                
                await SendAsync(notFoundResponse, 404, ct);
                return;
            }
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new PatchPatientResponse
            {
                Success = false,
                Message = "Patient patch not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Patient patch attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while patching patient with ID: {Id}", req.Id);
            
            var response = new PatchPatientResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
