using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Patients;

/// <summary>
/// Request model for adding a diagnosis to a patient.
/// </summary>
public class AddPatientDiagnosisRequest
{
    /// <summary>
    /// The unique identifier of the patient.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The start of care date for this diagnosis (format: MM/dd/yyyy).
    /// </summary>
    public string? StartOfCare { get; set; }

    /// <summary>
    /// The first diagnosis code and description.
    /// </summary>
    public string? FirstDiagnosis { get; set; }

    /// <summary>
    /// The second diagnosis code and description.
    /// </summary>
    public string? SecondDiagnosis { get; set; }
}

/// <summary>
/// Response model for adding a patient diagnosis.
/// </summary>
public class AddPatientDiagnosisResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? DiagnosisId { get; set; }
}

/// <summary>
/// Endpoint to add a diagnosis to a patient.
/// </summary>
public class AddPatientDiagnosisEndpoint : Endpoint<AddPatientDiagnosisRequest, AddPatientDiagnosisResponse>
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<AddPatientDiagnosisEndpoint> _logger;

    public AddPatientDiagnosisEndpoint(IPatientRepository repository, ILogger<AddPatientDiagnosisEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/patients/{id}/diagnoses");
        Policies("WriteAccess");
        Options(x => x
            .WithTags("Patients")
            .WithSummary("Add a diagnosis to a patient")
            .WithDescription("Adds a new episode diagnosis to a patient. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<AddPatientDiagnosisResponse>(201, "application/json")
            .Produces<AddPatientDiagnosisResponse>(404, "application/json")
            .Produces<AddPatientDiagnosisResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(AddPatientDiagnosisRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to add diagnosis to patient with ID: {Id}", req.Id);
            
            // Check if patient exists
            var patient = await _repository.GetByIdAsync(req.Id, ct);
            
            if (patient == null)
            {
                _logger.LogWarning("Patient with ID {Id} not found", req.Id);
                
                var notFoundResponse = new AddPatientDiagnosisResponse
                {
                    Success = false,
                    Message = $"Patient with ID {req.Id} not found"
                };
                
                await SendAsync(notFoundResponse, 404, ct);
                return;
            }
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new AddPatientDiagnosisResponse
            {
                Success = false,
                Message = "Adding patient diagnosis not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Adding patient diagnosis attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding diagnosis to patient with ID: {Id}", req.Id);
            
            var response = new AddPatientDiagnosisResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
