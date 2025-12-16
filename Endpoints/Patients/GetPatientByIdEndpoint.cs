using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Endpoints.Patients;

/// <summary>
/// Request model for getting a patient by ID.
/// </summary>
public class GetPatientByIdRequest
{
    /// <summary>
    /// The unique identifier of the patient.
    /// </summary>
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to retrieve a patient by their ID.
/// </summary>
public class GetPatientByIdEndpoint : Endpoint<GetPatientByIdRequest, Patient>
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<GetPatientByIdEndpoint> _logger;

    public GetPatientByIdEndpoint(IPatientRepository repository, ILogger<GetPatientByIdEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/patients/{id}");
        AllowAnonymous();
        Options(x => x
            .WithTags("Patients")
            .WithSummary("Get patient by ID")
            .WithDescription("Retrieves a specific patient by their unique identifier")
            .Produces<Patient>(200, "application/json")
            .Produces(StatusCodes.Status404NotFound, typeof(ProblemDetails))
            .Produces(StatusCodes.Status500InternalServerError, typeof(ProblemDetails)));
    }

    public override async Task HandleAsync(GetPatientByIdRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching patient with ID: {Id}", req.Id);
            
            var patient = await _repository.GetByIdAsync(req.Id, ct);
            
            if (patient == null)
            {
                _logger.LogWarning("Patient with ID {Id} not found", req.Id);
                await SendNotFoundAsync(ct);
                return;
            }
            
            _logger.LogInformation("Successfully retrieved patient with ID: {Id}", req.Id);
            
            await SendAsync(patient, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching patient with ID: {Id}", req.Id);
            
            await SendAsync(null!, 500, ct);
        }
    }
}
