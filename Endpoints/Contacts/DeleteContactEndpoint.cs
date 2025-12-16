using FastEndpoints;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Contacts;

/// <summary>
/// Request model for deleting a contact user.
/// </summary>
public class DeleteContactRequest
{
    /// <summary>
    /// The unique identifier of the contact user to delete.
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Response model for contact user deletion.
/// </summary>
public class DeleteContactResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to delete a contact user.
/// </summary>
public class DeleteContactEndpoint : Endpoint<DeleteContactRequest, DeleteContactResponse>
{
    private readonly IContactUserRepository _repository;
    private readonly ILogger<DeleteContactEndpoint> _logger;

    public DeleteContactEndpoint(IContactUserRepository repository, ILogger<DeleteContactEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Delete("/contacts/{id}");
        AllowAnonymous();
        Options(x => x
            .WithTags("Contacts")
            .WithSummary("Delete a contact user")
            .WithDescription("Deletes a contact user from the system. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<DeleteContactResponse>(200, "application/json")
            .Produces<DeleteContactResponse>(404, "application/json")
            .Produces<DeleteContactResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(DeleteContactRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to delete contact user with ID: {Id}", req.Id);
            
            // Check if contact exists
            var existingContact = await _repository.GetByIdAsync(req.Id, ct);
            
            if (existingContact == null)
            {
                _logger.LogWarning("Contact user with ID {Id} not found", req.Id);
                
                var notFoundResponse = new DeleteContactResponse
                {
                    Success = false,
                    Message = $"Contact user with ID {req.Id} not found"
                };
                
                await SendAsync(notFoundResponse, 404, ct);
                return;
            }
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new DeleteContactResponse
            {
                Success = false,
                Message = "Contact user deletion not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Contact user deletion attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting contact user with ID: {Id}", req.Id);
            
            var response = new DeleteContactResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
