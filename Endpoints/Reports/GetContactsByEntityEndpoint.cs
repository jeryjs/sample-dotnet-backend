using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Endpoints.Reports;

/// <summary>
/// DTO for a group of contacts by entity.
/// </summary>
public class ContactsByEntityGroup
{
    /// <summary>
    /// Gets the entity name.
    /// </summary>
    public string EntityName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the entity type.
    /// </summary>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the count of contacts in this entity.
    /// </summary>
    public int ContactCount { get; init; }

    /// <summary>
    /// Gets the list of contacts in this entity.
    /// </summary>
    public List<ContactSummary> Contacts { get; init; } = new();
}

/// <summary>
/// DTO for contact summary information.
/// </summary>
public class ContactSummary
{
    /// <summary>
    /// Gets the contact ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the contact WAV ID.
    /// </summary>
    public string ContactWavId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the contact's full name.
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the contact's job title.
    /// </summary>
    public string? JobTitle { get; init; }

    /// <summary>
    /// Gets the contact's email.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets the contact's lifecycle stage.
    /// </summary>
    public string? LifecycleStage { get; init; }
}

/// <summary>
/// Response DTO for contacts grouped by associated entity.
/// </summary>
public class ContactsByEntityResponse
{
    /// <summary>
    /// Gets the list of contact groups by entity.
    /// </summary>
    public List<ContactsByEntityGroup> Groups { get; init; } = new();

    /// <summary>
    /// Gets the total number of contacts across all entities.
    /// </summary>
    public int TotalContacts { get; init; }

    /// <summary>
    /// Gets the total number of entities.
    /// </summary>
    public int TotalEntities { get; init; }
}

/// <summary>
/// Endpoint to retrieve contacts grouped by associated entity.
/// </summary>
public class GetContactsByEntityEndpoint : EndpointWithoutRequest<ContactsByEntityResponse>
{
    private readonly IContactUserRepository _repository;
    private readonly ILogger<GetContactsByEntityEndpoint> _logger;

    public GetContactsByEntityEndpoint(IContactUserRepository repository, ILogger<GetContactsByEntityEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/reports/contacts-by-entity");
        AllowAnonymous();
        Options(x => x
            .WithTags("Reports")
            .WithSummary("Get contacts grouped by entity")
            .WithDescription("Retrieves all contacts grouped by their associated entity with summary information")
            .Produces<ContactsByEntityResponse>(200, "application/json")
            .Produces(StatusCodes.Status500InternalServerError, typeof(ProblemDetails)));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching contacts grouped by entity");
            
            var contacts = await _repository.GetAllAsync(ct);
            
            // Flatten contacts with their associated entities
            var contactEntityPairs = contacts
                .SelectMany(contact => contact.AssociatedEntities
                    .Where(entity => entity is not null)
                    .Select(entity => new { Contact = contact, Entity = entity! }))
                .ToList();
            
            var groups = contactEntityPairs
                .GroupBy(pair => new 
                { 
                    Name = pair.Entity?.Name ?? "No Entity", 
                    Type = pair.Entity?.EntityType ?? "Unknown" 
                })
                .Select(g => new ContactsByEntityGroup
                {
                    EntityName = g.Key.Name,
                    EntityType = g.Key.Type,
                    ContactCount = g.Count(),
                    Contacts = g.Select(pair => new ContactSummary
                    {
                        Id = pair.Contact.Id,
                        ContactWavId = pair.Contact.ContactWavId,
                        FullName = $"{pair.Contact.FirstName} {pair.Contact.LastName}".Trim(),
                        JobTitle = pair.Contact.JobTitle,
                        Email = pair.Contact.Email,
                        LifecycleStage = pair.Contact.ContactLifecycleStage
                    }).ToList()
                })
                .OrderByDescending(g => g.ContactCount)
                .ToList();
            
            var response = new ContactsByEntityResponse
            {
                Groups = groups,
                TotalContacts = contacts.Count,
                TotalEntities = groups.Count
            };
            
            _logger.LogInformation("Successfully retrieved contacts by entity: {EntityCount} entities, {ContactCount} contacts", 
                response.TotalEntities, response.TotalContacts);
            
            await SendAsync(response, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching contacts by entity");
            
            await SendAsync(new ContactsByEntityResponse(), 500, ct);
        }
    }
}
