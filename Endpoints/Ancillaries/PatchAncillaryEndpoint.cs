using FastEndpoints;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Ancillaries;

/// <summary>
/// Request model for partially updating an ancillary user.
/// </summary>
public class PatchAncillaryRequest
{
    /// <summary>
    /// The unique identifier of the ancillary user to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The entity WAV identifier (optional).
    /// </summary>
    public string? EntityWavId { get; set; }

    /// <summary>
    /// The name of the ancillary entity (optional).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The type of entity (optional).
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// The subtype of the entity (optional).
    /// </summary>
    public string? EntitySubtype { get; set; }

    /// <summary>
    /// The lifecycle stage of the entity (optional).
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
    /// The type of address (optional).
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
}

/// <summary>
/// Response model for ancillary user patch.
/// </summary>
public class PatchAncillaryResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to partially update an existing ancillary user.
/// </summary>
public class PatchAncillaryEndpoint : Endpoint<PatchAncillaryRequest, PatchAncillaryResponse>
{
    private readonly IAncillaryUserRepository _repository;
    private readonly ILogger<PatchAncillaryEndpoint> _logger;

    public PatchAncillaryEndpoint(IAncillaryUserRepository repository, ILogger<PatchAncillaryEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Patch("/ancillaries/{id}");
        AllowAnonymous();
        Options(x => x
            .WithTags("Ancillaries")
            .WithSummary("Partially update an ancillary user")
            .WithDescription("Partially updates an existing ancillary user with only the provided fields. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<PatchAncillaryResponse>(200, "application/json")
            .Produces<PatchAncillaryResponse>(404, "application/json")
            .Produces<PatchAncillaryResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(PatchAncillaryRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to patch ancillary user with ID: {Id}", req.Id);
            
            // Check if ancillary exists
            var existingAncillary = await _repository.GetByIdAsync(req.Id, ct);
            
            if (existingAncillary == null)
            {
                _logger.LogWarning("Ancillary user with ID {Id} not found", req.Id);
                
                var notFoundResponse = new PatchAncillaryResponse
                {
                    Success = false,
                    Message = $"Ancillary user with ID {req.Id} not found"
                };
                
                await SendAsync(notFoundResponse, 404, ct);
                return;
            }
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new PatchAncillaryResponse
            {
                Success = false,
                Message = "Ancillary user patch not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Ancillary user patch attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while patching ancillary user with ID: {Id}", req.Id);
            
            var response = new PatchAncillaryResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
