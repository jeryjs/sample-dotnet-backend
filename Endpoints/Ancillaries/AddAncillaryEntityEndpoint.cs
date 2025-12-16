using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Ancillaries;

/// <summary>
/// Request model for adding an associated entity to an ancillary user.
/// </summary>
public class AddAncillaryEntityRequest
{
    /// <summary>
    /// The unique identifier of the ancillary user.
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
public class AddAncillaryEntityResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to add an associated entity to an ancillary user.
/// </summary>
public class AddAncillaryEntityEndpoint : Endpoint<AddAncillaryEntityRequest, AddAncillaryEntityResponse>
{
    private readonly IAncillaryUserRepository _repository;
    private readonly ILogger<AddAncillaryEntityEndpoint> _logger;

    public AddAncillaryEntityEndpoint(IAncillaryUserRepository repository, ILogger<AddAncillaryEntityEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/ancillaries/{id}/entities");
        Policies("WriteAccess");
        Options(x => x
            .WithTags("Ancillaries")
            .WithSummary("Add an associated entity to an ancillary user")
            .WithDescription("Adds a new associated entity to an ancillary user. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<AddAncillaryEntityResponse>(201, "application/json")
            .Produces<AddAncillaryEntityResponse>(404, "application/json")
            .Produces<AddAncillaryEntityResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(AddAncillaryEntityRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to add associated entity to ancillary user with ID: {Id}", req.Id);
            
            // Check if ancillary exists
            var existingAncillary = await _repository.GetByIdAsync(req.Id, ct);
            
            if (existingAncillary == null)
            {
                _logger.LogWarning("Ancillary user with ID {Id} not found", req.Id);
                
                var notFoundResponse = new AddAncillaryEntityResponse
                {
                    Success = false,
                    Message = $"Ancillary user with ID {req.Id} not found"
                };
                
                await SendAsync(notFoundResponse, 404, ct);
                return;
            }
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new AddAncillaryEntityResponse
            {
                Success = false,
                Message = "Adding associated entity not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Adding associated entity attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding associated entity to ancillary user with ID: {Id}", req.Id);
            
            var response = new AddAncillaryEntityResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
