using FastEndpoints;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Ancillaries;

/// <summary>
/// Request model for deleting an ancillary user.
/// </summary>
public class DeleteAncillaryRequest
{
    /// <summary>
    /// The unique identifier of the ancillary user to delete.
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Response model for ancillary user deletion.
/// </summary>
public class DeleteAncillaryResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to delete an ancillary user.
/// </summary>
public class DeleteAncillaryEndpoint : Endpoint<DeleteAncillaryRequest, DeleteAncillaryResponse>
{
    private readonly IAncillaryUserRepository _repository;
    private readonly ILogger<DeleteAncillaryEndpoint> _logger;

    public DeleteAncillaryEndpoint(IAncillaryUserRepository repository, ILogger<DeleteAncillaryEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Delete("/ancillaries/{id}");
        AllowAnonymous();
        Options(x => x
            .WithTags("Ancillaries")
            .WithSummary("Delete an ancillary user")
            .WithDescription("Deletes an ancillary user from the system. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<DeleteAncillaryResponse>(200, "application/json")
            .Produces<DeleteAncillaryResponse>(404, "application/json")
            .Produces<DeleteAncillaryResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(DeleteAncillaryRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to delete ancillary user with ID: {Id}", req.Id);
            
            // Check if ancillary exists
            var existingAncillary = await _repository.GetByIdAsync(req.Id, ct);
            
            if (existingAncillary == null)
            {
                _logger.LogWarning("Ancillary user with ID {Id} not found", req.Id);
                
                var notFoundResponse = new DeleteAncillaryResponse
                {
                    Success = false,
                    Message = $"Ancillary user with ID {req.Id} not found"
                };
                
                await SendAsync(notFoundResponse, 404, ct);
                return;
            }
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new DeleteAncillaryResponse
            {
                Success = false,
                Message = "Ancillary user deletion not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Ancillary user deletion attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting ancillary user with ID: {Id}", req.Id);
            
            var response = new DeleteAncillaryResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
