using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Patients;

/// <summary>
/// Request model for getting patient diagnoses.
/// </summary>
public class GetPatientDiagnosesRequest
{
    /// <summary>
    /// The unique identifier of the patient.
    /// </summary>
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Response model for patient diagnoses.
/// </summary>
public class GetPatientDiagnosesResponse
{
    public string PatientId { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public List<EpisodeDiagnosis> Diagnoses { get; set; } = new();
}

/// <summary>
/// Endpoint to retrieve diagnoses for a specific patient.
/// </summary>
public class GetPatientDiagnosesEndpoint : Endpoint<GetPatientDiagnosesRequest, GetPatientDiagnosesResponse>
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<GetPatientDiagnosesEndpoint> _logger;

    public GetPatientDiagnosesEndpoint(IPatientRepository repository, ILogger<GetPatientDiagnosesEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/patients/{id}/diagnoses");
        AllowAnonymous();
        Options(x => x
            .WithTags("Patients")
            .WithSummary("Get patient diagnoses")
            .WithDescription("Retrieves all episode diagnoses for a specific patient")
            .Produces<GetPatientDiagnosesResponse>(200, "application/json")
            .Produces(404, "application/json")
            .Produces(500, "application/json"));
    }

    public override async Task HandleAsync(GetPatientDiagnosesRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching diagnoses for patient with ID: {Id}", req.Id);
            
            var patient = await _repository.GetByIdAsync(req.Id, ct);
            
            if (patient == null)
            {
                _logger.LogWarning("Patient with ID {Id} not found", req.Id);
                await SendNotFoundAsync(ct);
                return;
            }
            
            var diagnoses = patient.AgencyInfo?.EpisodeDiagnoses?.ToList() ?? new List<EpisodeDiagnosis>();
            
            var response = new GetPatientDiagnosesResponse
            {
                PatientId = patient.Id,
                PatientName = $"{patient.AgencyInfo?.PatientFName} {patient.AgencyInfo?.PatientLName}".Trim(),
                Diagnoses = diagnoses
            };
            
            _logger.LogInformation("Successfully retrieved {Count} diagnoses for patient {Id}", diagnoses.Count, req.Id);
            
            await SendAsync(response, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching diagnoses for patient with ID: {Id}", req.Id);
            
            await SendAsync(null!, 500, ct);
        }
    }
}
