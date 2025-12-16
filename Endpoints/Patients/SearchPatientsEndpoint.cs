using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Patients;

/// <summary>
/// Request model for searching patients.
/// </summary>
public class SearchPatientsRequest
{
    /// <summary>
    /// First name to search for.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name to search for.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Email to search for.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Phone number to search for.
    /// </summary>
    public string? Phone { get; set; }
}

/// <summary>
/// Endpoint to search for patients based on various criteria.
/// </summary>
public class SearchPatientsEndpoint : Endpoint<SearchPatientsRequest, List<Patient>>
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<SearchPatientsEndpoint> _logger;

    public SearchPatientsEndpoint(IPatientRepository repository, ILogger<SearchPatientsEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/search/patients");
        AllowAnonymous();
        Options(x => x
            .WithTags("Patients")
            .WithSummary("Search patients")
            .WithDescription("Search for patients by first name, last name, email, or phone number")
            .Produces<List<Patient>>(200, "application/json")
            .Produces(500, "application/json"));
    }

    public override async Task HandleAsync(SearchPatientsRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Searching patients with criteria - FirstName: {FirstName}, LastName: {LastName}, Email: {Email}, Phone: {Phone}",
                req.FirstName ?? "N/A", 
                req.LastName ?? "N/A", 
                req.Email ?? "N/A", 
                req.Phone ?? "N/A");
            
            var patients = await _repository.SearchAsync(
                req.FirstName, 
                req.LastName, 
                req.Email, 
                req.Phone, 
                ct);
            
            _logger.LogInformation("Search returned {Count} patients", patients.Count);
            
            await SendAsync(patients, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching patients");
            
            await SendAsync(new List<Patient>(), 500, ct);
        }
    }
}
