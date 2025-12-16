using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Endpoints.Contacts;

/// <summary>
/// Request model for getting a contact user by ID.
/// </summary>
public class GetContactByIdRequest
{
    /// <summary>
    /// The unique identifier of the contact user.
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Endpoint to retrieve a contact user by their ID.
/// </summary>
public class GetContactByIdEndpoint : Endpoint<GetContactByIdRequest, ContactUser>
{
    private readonly IContactUserRepository _repository;
    private readonly ILogger<GetContactByIdEndpoint> _logger;

    public GetContactByIdEndpoint(IContactUserRepository repository, ILogger<GetContactByIdEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/contacts/{id}");
        Policies("ReadAccess");
        Options(x => x
            .WithTags("Contacts")
            .WithSummary("Get contact user by ID")
            .WithDescription("Retrieves a specific contact user by their unique identifier")
            .Produces<ContactUser>(200, "application/json")
            .Produces(StatusCodes.Status404NotFound, typeof(ProblemDetails))
            .Produces(StatusCodes.Status500InternalServerError, typeof(ProblemDetails)));
    }

    public override async Task HandleAsync(GetContactByIdRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching contact user with ID: {Id}", req.Id);
            
            var contact = await _repository.GetByIdAsync(req.Id, ct);
            
            if (contact == null)
            {
                _logger.LogWarning("Contact user with ID {Id} not found", req.Id);
                await SendNotFoundAsync(ct);
                return;
            }
            
            _logger.LogInformation("Successfully retrieved contact user with ID: {Id}", req.Id);
            
            await SendAsync(contact, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching contact user with ID: {Id}", req.Id);
            
            await SendAsync(null!, 500, ct);
        }
    }
}
