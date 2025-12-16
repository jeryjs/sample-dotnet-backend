using FastEndpoints;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Patients;

/// <summary>
/// Request model for deleting a patient.
/// </summary>
public class DeletePatientRequest
{
    /// <summary>
    /// The unique identifier of the patient to delete.
    /// </summary>
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Response model for patient deletion.
/// </summary>
public class DeletePatientResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to delete a patient.
/// </summary>
public class DeletePatientEndpoint : Endpoint<DeletePatientRequest, DeletePatientResponse>
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<DeletePatientEndpoint> _logger;

    public DeletePatientEndpoint(IPatientRepository repository, ILogger<DeletePatientEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Delete("/patients/{id}");
        AllowAnonymous();
        Options(x => x
            .WithTags("Patients")
            .WithSummary("Delete a patient")
            .WithDescription("Deletes a patient from the system. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<DeletePatientResponse>(200, "application/json")
            .Produces<DeletePatientResponse>(404, "application/json")
            .Produces<DeletePatientResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(DeletePatientRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to delete patient with ID: {Id}", req.Id);
            
            // Check if patient exists
            var existingPatient = await _repository.GetByIdAsync(req.Id, ct);
            
            if (existingPatient == null)
            {
                _logger.LogWarning("Patient with ID {Id} not found", req.Id);
                
                var notFoundResponse = new DeletePatientResponse
                {
                    Success = false,
                    Message = $"Patient with ID {req.Id} not found"
                };
                
                await SendAsync(notFoundResponse, 404, ct);
                return;
            }
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new DeletePatientResponse
            {
                Success = false,
                Message = "Patient deletion not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Patient deletion attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting patient with ID: {Id}", req.Id);
            
            var response = new DeletePatientResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
