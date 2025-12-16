using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Endpoints.Ancillaries;

/// <summary>
/// Endpoint to retrieve all ancillary users.
/// </summary>
public class GetAllAncillariesEndpoint : EndpointWithoutRequest<List<AncillaryUser>>
{
    private readonly IAncillaryUserRepository _repository;
    private readonly ILogger<GetAllAncillariesEndpoint> _logger;

    public GetAllAncillariesEndpoint(IAncillaryUserRepository repository, ILogger<GetAllAncillariesEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/ancillaries");
        AllowAnonymous();
        Options(x => x
            .WithTags("Ancillaries")
            .WithSummary("Get all ancillary users")
            .WithDescription("Retrieves all ancillary users from the system without pagination")
            .Produces<List<AncillaryUser>>(200, "application/json")
            .Produces(StatusCodes.Status500InternalServerError, typeof(ProblemDetails)));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching all ancillary users");
            
            var ancillaries = await _repository.GetAllAsync(ct);
            
            _logger.LogInformation("Successfully retrieved {Count} ancillary users", ancillaries.Count);
            
            await SendAsync(ancillaries, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all ancillary users");
            
            await SendAsync(new List<AncillaryUser>(), 500, ct);
        }
    }
}
