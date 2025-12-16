using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Ancillaries;

/// <summary>
/// Request model for creating an ancillary user.
/// </summary>
public class CreateAncillaryRequest
{
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
/// Response model for ancillary user creation.
/// </summary>
public class CreateAncillaryResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? AncillaryId { get; set; }
}

/// <summary>
/// Endpoint to create a new ancillary user.
/// </summary>
public class CreateAncillaryEndpoint : Endpoint<CreateAncillaryRequest, CreateAncillaryResponse>
{
    private readonly ILogger<CreateAncillaryEndpoint> _logger;

    public CreateAncillaryEndpoint(ILogger<CreateAncillaryEndpoint> logger)
    {
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/ancillaries");
        AllowAnonymous();
        Options(x => x
            .WithTags("Ancillaries")
            .WithSummary("Create a new ancillary user")
            .WithDescription("Creates a new ancillary user in the system. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<CreateAncillaryResponse>(201, "application/json")
            .Produces<CreateAncillaryResponse>(400, "application/json")
            .Produces<CreateAncillaryResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(CreateAncillaryRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to create new ancillary user with name: {Name}", req.Name);
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new CreateAncillaryResponse
            {
                Success = false,
                Message = "Ancillary user creation not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Ancillary user creation attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating ancillary user");
            
            var response = new CreateAncillaryResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
