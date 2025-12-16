using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Endpoints.Ancillaries;

/// <summary>
/// Request model for getting ancillary user's associated entities.
/// </summary>
public class GetAncillaryEntitiesRequest
{
    /// <summary>
    /// The unique identifier of the ancillary user.
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Endpoint to retrieve all associated entities for an ancillary user.
/// </summary>
public class GetAncillaryEntitiesEndpoint : Endpoint<GetAncillaryEntitiesRequest, List<AssociatedEntity>>
{
    private readonly IAncillaryUserRepository _repository;
    private readonly ILogger<GetAncillaryEntitiesEndpoint> _logger;

    public GetAncillaryEntitiesEndpoint(IAncillaryUserRepository repository, ILogger<GetAncillaryEntitiesEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/ancillaries/{id}/entities");
        AllowAnonymous();
        Options(x => x
            .WithTags("Ancillaries")
            .WithSummary("Get ancillary user's associated entities")
            .WithDescription("Retrieves all associated entities for a specific ancillary user")
            .Produces<List<AssociatedEntity>>(200, "application/json")
            .Produces(StatusCodes.Status404NotFound, typeof(ProblemDetails))
            .Produces(StatusCodes.Status500InternalServerError, typeof(ProblemDetails)));
    }

    public override async Task HandleAsync(GetAncillaryEntitiesRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching associated entities for ancillary user with ID: {Id}", req.Id);
            
            var ancillary = await _repository.GetByIdAsync(req.Id, ct);
            
            if (ancillary == null)
            {
                _logger.LogWarning("Ancillary user with ID {Id} not found", req.Id);
                await SendNotFoundAsync(ct);
                return;
            }
            
            var entities = ancillary.AssociatedEntities.ToList();
            
            _logger.LogInformation("Successfully retrieved {Count} associated entities for ancillary user with ID: {Id}", 
                entities.Count, req.Id);
            
            await SendAsync(entities, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching associated entities for ancillary user with ID: {Id}", req.Id);
            
            await SendAsync(new List<AssociatedEntity>(), 500, ct);
        }
    }
}
