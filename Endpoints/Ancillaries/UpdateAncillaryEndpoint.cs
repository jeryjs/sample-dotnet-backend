using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Ancillaries;

/// <summary>
/// Request model for updating an ancillary user.
/// </summary>
public class UpdateAncillaryRequest
{
    /// <summary>
    /// The unique identifier of the ancillary user to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The entity WAV identifier.
    /// </summary>
    public required string EntityWavId { get; set; }

    /// <summary>
    /// The name of the ancillary entity.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The type of entity (e.g., "ANCILLIARY").
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// The subtype of the entity (e.g., "Home Health Agency") (optional).
    /// </summary>
    public string? EntitySubtype { get; set; }

    /// <summary>
    /// The lifecycle stage of the entity (e.g., "Freemium") (optional).
    /// </summary>
    public string? LifecycleStage { get; set; }

    /// <summary>
    /// The National Provider Identifier (NPI) number (optional).
    /// </summary>
    public string? EntityNpiNumber { get; set; }

    /// <summary>
    /// The clinical services provided (optional).
    /// </summary>
    public string? ClinicalServices { get; set; }

    /// <summary>
    /// The services provided as a string (optional).
    /// </summary>
    public string? Services { get; set; }

    /// <summary>
    /// The services provided as an array (optional).
    /// </summary>
    public List<string>? ServicesArray { get; set; }

    /// <summary>
    /// The type of address (e.g., "PRIMARY") (optional).
    /// </summary>
    public string? AddressType { get; set; }

    /// <summary>
    /// The state where the ancillary entity is located (optional).
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// The city where the ancillary entity is located (optional).
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// The zip code of the ancillary entity (optional).
    /// </summary>
    public string? Zipcode { get; set; }

    /// <summary>
    /// The latitude coordinate of the ancillary entity location (optional).
    /// </summary>
    public string? Lat { get; set; }

    /// <summary>
    /// The longitude coordinate of the ancillary entity location (optional).
    /// </summary>
    public string? Lon { get; set; }

    /// <summary>
    /// The email address of the ancillary entity (optional).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The phone number of the ancillary entity (optional).
    /// </summary>
    public string? PhoneNo { get; set; }

    /// <summary>
    /// Collection of associated entities (optional).
    /// </summary>
    public List<AssociatedEntity>? AssociatedEntities { get; set; }
}

/// <summary>
/// Response model for ancillary user update.
/// </summary>
public class UpdateAncillaryResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to update an existing ancillary user.
/// </summary>
public class UpdateAncillaryEndpoint : Endpoint<UpdateAncillaryRequest, UpdateAncillaryResponse>
{
    private readonly IAncillaryUserRepository _repository;
    private readonly ILogger<UpdateAncillaryEndpoint> _logger;

    public UpdateAncillaryEndpoint(IAncillaryUserRepository repository, ILogger<UpdateAncillaryEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Put("/ancillaries/{id}");
        Policies("WriteAccess");
        Options(x => x
            .WithTags("Ancillaries")
            .WithSummary("Update an ancillary user")
            .WithDescription("Updates an existing ancillary user with all provided fields. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<UpdateAncillaryResponse>(200, "application/json")
            .Produces<UpdateAncillaryResponse>(404, "application/json")
            .Produces<UpdateAncillaryResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(UpdateAncillaryRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to update ancillary user with ID: {Id}", req.Id);
            
            // Check if ancillary exists
            var existingAncillary = await _repository.GetByIdAsync(req.Id, ct);
            
            if (existingAncillary == null)
            {
                _logger.LogWarning("Ancillary user with ID {Id} not found", req.Id);
                
                var notFoundResponse = new UpdateAncillaryResponse
                {
                    Success = false,
                    Message = $"Ancillary user with ID {req.Id} not found"
                };
                
                await SendAsync(notFoundResponse, 404, ct);
                return;
            }
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new UpdateAncillaryResponse
            {
                Success = false,
                Message = "Ancillary user update not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Ancillary user update attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating ancillary user with ID: {Id}", req.Id);
            
            var response = new UpdateAncillaryResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
