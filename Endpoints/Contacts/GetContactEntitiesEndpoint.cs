using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Endpoints.Contacts;

/// <summary>
/// Request model for getting contact user's associated entities.
/// </summary>
public class GetContactEntitiesRequest
{
    /// <summary>
    /// The unique identifier of the contact user.
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Endpoint to retrieve all associated entities for a contact user.
/// </summary>
public class GetContactEntitiesEndpoint : Endpoint<GetContactEntitiesRequest, List<AssociatedEntity>>
{
    private readonly IContactUserRepository _repository;
    private readonly ILogger<GetContactEntitiesEndpoint> _logger;

    public GetContactEntitiesEndpoint(IContactUserRepository repository, ILogger<GetContactEntitiesEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/contacts/{id}/entities");
        Policies("ReadAccess");
        Options(x => x
            .WithTags("Contacts")
            .WithSummary("Get contact user's associated entities")
            .WithDescription("Retrieves all associated entities for a specific contact user")
            .Produces<List<AssociatedEntity>>(200, "application/json")
            .Produces(StatusCodes.Status404NotFound, typeof(ProblemDetails))
            .Produces(StatusCodes.Status500InternalServerError, typeof(ProblemDetails)));
    }

    public override async Task HandleAsync(GetContactEntitiesRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching associated entities for contact user with ID: {Id}", req.Id);
            
            var contact = await _repository.GetByIdAsync(req.Id, ct);
            
            if (contact == null)
            {
                _logger.LogWarning("Contact user with ID {Id} not found", req.Id);
                await SendNotFoundAsync(ct);
                return;
            }
            
            var entities = contact.AssociatedEntities.ToList();
            
            _logger.LogInformation("Successfully retrieved {Count} associated entities for contact user with ID: {Id}", 
                entities.Count, req.Id);
            
            await SendAsync(entities, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching associated entities for contact user with ID: {Id}", req.Id);
            
            await SendAsync(new List<AssociatedEntity>(), 500, ct);
        }
    }
}
