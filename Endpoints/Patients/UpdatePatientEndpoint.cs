using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Patients;

/// <summary>
/// Request model for updating a patient.
/// </summary>
public class UpdatePatientRequest
{
    /// <summary>
    /// The unique identifier of the patient to update.
    /// </summary>
    public string Id { get; set; } = string.Empty;

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
/// Response model for patient update.
/// </summary>
public class UpdatePatientResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to update an existing patient (full update).
/// </summary>
public class UpdatePatientEndpoint : Endpoint<UpdatePatientRequest, UpdatePatientResponse>
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<UpdatePatientEndpoint> _logger;

    public UpdatePatientEndpoint(IPatientRepository repository, ILogger<UpdatePatientEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Put("/patients/{id}");
        AllowAnonymous();
        Options(x => x
            .WithTags("Patients")
            .WithSummary("Update a patient")
            .WithDescription("Updates an existing patient with all provided fields. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<UpdatePatientResponse>(200, "application/json")
            .Produces<UpdatePatientResponse>(404, "application/json")
            .Produces<UpdatePatientResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(UpdatePatientRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to update patient with ID: {Id}", req.Id);
            
            // Check if patient exists
            var existingPatient = await _repository.GetByIdAsync(req.Id, ct);
            
            if (existingPatient == null)
            {
                _logger.LogWarning("Patient with ID {Id} not found", req.Id);
                
                var notFoundResponse = new UpdatePatientResponse
                {
                    Success = false,
                    Message = $"Patient with ID {req.Id} not found"
                };
                
                await SendAsync(notFoundResponse, 404, ct);
                return;
            }
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new UpdatePatientResponse
            {
                Success = false,
                Message = "Patient update not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Patient update attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating patient with ID: {Id}", req.Id);
            
            var response = new UpdatePatientResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
