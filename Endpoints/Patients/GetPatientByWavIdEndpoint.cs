using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Endpoints.Patients;

/// <summary>
/// Request model for getting a patient by WAV ID.
/// </summary>
public class GetPatientByWavIdRequest
{
    /// <summary>
    /// The WAV identifier of the patient.
    /// </summary>
    public string WavId { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to retrieve a patient by their WAV ID.
/// </summary>
public class GetPatientByWavIdEndpoint : Endpoint<GetPatientByWavIdRequest, Patient>
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<GetPatientByWavIdEndpoint> _logger;

    public GetPatientByWavIdEndpoint(IPatientRepository repository, ILogger<GetPatientByWavIdEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/patients/wavid/{wavId}");
        Policies("ReadAccess");
        Options(x => x
            .WithTags("Patients")
            .WithSummary("Get patient by WAV ID")
            .WithDescription("Retrieves a specific patient by their WAV identifier")
            .Produces<Patient>(200, "application/json")
            .Produces(StatusCodes.Status404NotFound, typeof(ProblemDetails))
            .Produces(StatusCodes.Status500InternalServerError, typeof(ProblemDetails)));
    }

    public override async Task HandleAsync(GetPatientByWavIdRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching patient with WAV ID: {WavId}", req.WavId);
            
            var patient = await _repository.GetByWavIdAsync(req.WavId, ct);
            
            if (patient == null)
            {
                _logger.LogWarning("Patient with WAV ID {WavId} not found", req.WavId);
                await SendNotFoundAsync(ct);
                return;
            }
            
            _logger.LogInformation("Successfully retrieved patient with WAV ID: {WavId}", req.WavId);
            
            await SendAsync(patient, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching patient with WAV ID: {WavId}", req.WavId);
            
            await SendAsync(null!, 500, ct);
        }
    }
}
