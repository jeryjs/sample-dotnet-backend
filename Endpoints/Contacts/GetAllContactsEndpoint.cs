using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Endpoints.Contacts;

/// <summary>
/// Endpoint to retrieve all contact users.
/// </summary>
public class GetAllContactsEndpoint : EndpointWithoutRequest<List<ContactUser>>
{
    private readonly IContactUserRepository _repository;
    private readonly ILogger<GetAllContactsEndpoint> _logger;

    public GetAllContactsEndpoint(IContactUserRepository repository, ILogger<GetAllContactsEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/contacts");
        AllowAnonymous();
        Options(x => x
            .WithTags("Contacts")
            .WithSummary("Get all contact users")
            .WithDescription("Retrieves all contact users from the system without pagination")
            .Produces<List<ContactUser>>(200, "application/json")
            .Produces(StatusCodes.Status500InternalServerError, typeof(ProblemDetails)));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching all contact users");
            
            var contacts = await _repository.GetAllAsync(ct);
            
            _logger.LogInformation("Successfully retrieved {Count} contact users", contacts.Count);
            
            await SendAsync(contacts, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all contact users");
            
            await SendAsync(new List<ContactUser>(), 500, ct);
        }
    }
}
