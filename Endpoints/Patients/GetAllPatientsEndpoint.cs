using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

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
        AllowAnonymous();
        Options(x => x
            .WithTags("Patients")
            .WithSummary("Get all patients")
            .WithDescription("Retrieves all patients from the system without pagination")
            .Produces<List<Patient>>(200, "application/json")
            .Produces(500, "application/json"));
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
