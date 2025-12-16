using FastEndpoints;
using backend_api.Domain.Models;

namespace BackendApi.Endpoints.Bulk;

/// <summary>
/// Request model for bulk creating ancillaries.
/// </summary>
public class BulkCreateAncillariesRequest
{
    /// <summary>
    /// Array of ancillary data to create.
    /// </summary>
    public required List<AncillaryUserDto> Ancillaries { get; set; }
}

/// <summary>
/// DTO for ancillary creation in bulk operations.
/// </summary>
public class AncillaryUserDto
{
    /// <summary>
    /// Unique identifier for the ancillary user.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Entity WAV identifier.
    /// </summary>
    public required string EntityWavId { get; set; }

    /// <summary>
    /// Name of the ancillary entity.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Type of entity (e.g., "ANCILLIARY").
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// Subtype of the entity (e.g., "Home Health Agency").
    /// </summary>
    public string? EntitySubtype { get; set; }

    /// <summary>
    /// Lifecycle stage of the entity (e.g., "Freemium").
    /// </summary>
    public string? LifecycleStage { get; set; }

    /// <summary>
    /// National Provider Identifier (NPI) number.
    /// </summary>
    public string? EntityNpiNumber { get; set; }

    /// <summary>
    /// Clinical services provided.
    /// </summary>
    public string? ClinicalServices { get; set; }

    /// <summary>
    /// Services provided as a string.
    /// </summary>
    public string? Services { get; set; }

    /// <summary>
    /// Services provided as an array.
    /// </summary>
    public List<string>? ServicesArray { get; set; }

    /// <summary>
    /// Type of address (e.g., "PRIMARY").
    /// </summary>
    public string? AddressType { get; set; }

    /// <summary>
    /// State where the ancillary entity is located.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// City where the ancillary entity is located.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Zip code of the ancillary entity.
    /// </summary>
    public string? Zipcode { get; set; }

    /// <summary>
    /// Latitude coordinate of the ancillary entity location.
    /// </summary>
    public string? Lat { get; set; }

    /// <summary>
    /// Longitude coordinate of the ancillary entity location.
    /// </summary>
    public string? Lon { get; set; }

    /// <summary>
    /// Email address of the ancillary entity.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Phone number of the ancillary entity.
    /// </summary>
    public string? PhoneNo { get; set; }

    /// <summary>
    /// Collection of entities associated with this ancillary user.
    /// </summary>
    public List<AssociatedEntity> AssociatedEntities { get; set; } = new();
}

/// <summary>
/// Response model for bulk ancillary creation.
/// </summary>
public class BulkCreateAncillariesResponse
{
    /// <summary>
    /// Indicates whether the bulk operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result of the operation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Number of ancillaries successfully created.
    /// </summary>
    public int CreatedCount { get; set; }

    /// <summary>
    /// Number of ancillaries that failed to create.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// List of errors that occurred during bulk creation.
    /// </summary>
    public List<BulkOperationError> Errors { get; set; } = new();
}

/// <summary>
/// Endpoint to bulk create ancillaries.
/// </summary>
public class BulkCreateAncillariesEndpoint : Endpoint<BulkCreateAncillariesRequest, BulkCreateAncillariesResponse>
{
    private readonly ILogger<BulkCreateAncillariesEndpoint> _logger;

    public BulkCreateAncillariesEndpoint(ILogger<BulkCreateAncillariesEndpoint> logger)
    {
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/bulk/ancillaries");
        Policies("WriteAccess");
        Options(x => x
            .WithTags("Bulk Operations")
            .WithSummary("Bulk create ancillaries")
            .WithDescription("Creates multiple ancillaries in a single operation. Currently returns 501 Not Implemented.")
            .Produces<BulkCreateAncillariesResponse>(201, "application/json")
            .Produces<BulkCreateAncillariesResponse>(400, "application/json")
            .Produces<BulkCreateAncillariesResponse>(501, "application/json")
            .Produces<BulkCreateAncillariesResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(BulkCreateAncillariesRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Bulk ancillary creation attempted with {Count} ancillaries", req.Ancillaries?.Count ?? 0);

            // Validate request
            if (req.Ancillaries == null || req.Ancillaries.Count == 0)
            {
                var validationResponse = new BulkCreateAncillariesResponse
                {
                    Success = false,
                    Message = "Request must contain at least one ancillary.",
                    CreatedCount = 0,
                    ErrorCount = 0
                };

                await SendAsync(validationResponse, 400, ct);
                return;
            }

            var response = new BulkCreateAncillariesResponse
            {
                Success = false,
                Message = "Bulk ancillary creation is not implemented. Repository is currently read-only.",
                CreatedCount = 0,
                ErrorCount = 0
            };

            _logger.LogWarning("Bulk ancillary creation attempted but not implemented. Request contained {Count} ancillaries", req.Ancillaries.Count);

            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during bulk ancillary creation attempt");

            var response = new BulkCreateAncillariesResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}",
                CreatedCount = 0,
                ErrorCount = 0
            };

            await SendAsync(response, 500, ct);
        }
    }
}
