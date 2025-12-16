using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Endpoints.Patients;

/// <summary>
/// Endpoint to retrieve all patients.
/// </summary>
public class GetAllPatientsEndpoint : EndpointWithoutRequest<List<Patient>>
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<GetAllPatientsEndpoint> _logger;

    public GetAllPatientsEndpoint(IPatientRepository repository, ILogger<GetAllPatientsEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/patients");
        Policies("ReadAccess");
        Options(x => x
            .WithTags("Patients")
            .WithSummary("Get all patients")
            .WithDescription("Retrieves all patients from the system without pagination")
            .Produces<List<Patient>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError, typeof(ProblemDetails)));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching all patients");
            
            var patients = await _repository.GetAllAsync(ct);
            
            _logger.LogInformation("Successfully retrieved {Count} patients", patients.Count);
            
            await SendAsync(patients, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all patients");
            
            await SendAsync(new List<Patient>(), 500, ct);
        }
    }
}
