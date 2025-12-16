using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Contacts;

/// <summary>
/// Request model for adding an associated entity to a contact user.
/// </summary>
public class AddContactEntityRequest
{
    /// <summary>
    /// The unique identifier of the contact user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The associated entity to add.
    /// </summary>
    public required AssociatedEntity Entity { get; set; }
}

/// <summary>
/// Response model for adding an associated entity.
/// </summary>
public class AddContactEntityResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to add an associated entity to a contact user.
/// </summary>
public class AddContactEntityEndpoint : Endpoint<AddContactEntityRequest, AddContactEntityResponse>
{
    private readonly IContactUserRepository _repository;
    private readonly ILogger<AddContactEntityEndpoint> _logger;

    public AddContactEntityEndpoint(IContactUserRepository repository, ILogger<AddContactEntityEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/contacts/{id}/entities");
        AllowAnonymous();
        Options(x => x
            .WithTags("Contacts")
            .WithSummary("Add an associated entity to a contact user")
            .WithDescription("Adds a new associated entity to an existing contact user. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<AddContactEntityResponse>(201, "application/json")
            .Produces<AddContactEntityResponse>(404, "application/json")
            .Produces<AddContactEntityResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(AddContactEntityRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to add associated entity to contact user with ID: {Id}", req.Id);
            
            // Check if contact exists
            var existingContact = await _repository.GetByIdAsync(req.Id, ct);
            
            if (existingContact == null)
            {
                _logger.LogWarning("Contact user with ID {Id} not found", req.Id);
                
                var notFoundResponse = new AddContactEntityResponse
                {
                    Success = false,
                    Message = $"Contact user with ID {req.Id} not found"
                };
                
                await SendAsync(notFoundResponse, 404, ct);
                return;
            }
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new AddContactEntityResponse
            {
                Success = false,
                Message = "Adding associated entity not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Adding associated entity attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding associated entity to contact user with ID: {Id}", req.Id);
            
            var response = new AddContactEntityResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
