using FastEndpoints;
using backend_api.Domain.Models;

namespace BackendApi.Endpoints.Bulk;

/// <summary>
/// Request model for bulk creating contacts.
/// </summary>
public class BulkCreateContactsRequest
{
    /// <summary>
    /// Array of contact data to create.
    /// </summary>
    public required List<ContactUserDto> Contacts { get; set; }
}

/// <summary>
/// DTO for contact creation in bulk operations.
/// </summary>
public class ContactUserDto
{
    /// <summary>
    /// Unique identifier for the contact user.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Contact WAV identifier.
    /// </summary>
    public required string ContactWavId { get; set; }

    /// <summary>
    /// Collection of entities associated with this contact user.
    /// </summary>
    public List<AssociatedEntity> AssociatedEntities { get; set; } = new();

    /// <summary>
    /// First name of the contact user.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// Last name of the contact user.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Job title of the contact user.
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// Persona type of the contact user (e.g., "Neutral").
    /// </summary>
    public string? PersonaType { get; set; }

    /// <summary>
    /// Lifecycle stage of the contact (e.g., "User").
    /// </summary>
    public string? ContactLifecycleStage { get; set; }

    /// <summary>
    /// State where the contact user is located.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// City where the contact user is located.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Email address of the contact user.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Phone number of the contact user.
    /// </summary>
    public string? PhoneNo { get; set; }

    /// <summary>
    /// Email of the contact owner.
    /// </summary>
    public string? ContactOwner { get; set; }

    /// <summary>
    /// Indicates whether the contact user is active.
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Response model for bulk contact creation.
/// </summary>
public class BulkCreateContactsResponse
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
    /// Number of contacts successfully created.
    /// </summary>
    public int CreatedCount { get; set; }

    /// <summary>
    /// Number of contacts that failed to create.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// List of errors that occurred during bulk creation.
    /// </summary>
    public List<BulkOperationError> Errors { get; set; } = new();
}

/// <summary>
/// Endpoint to bulk create contacts.
/// </summary>
public class BulkCreateContactsEndpoint : Endpoint<BulkCreateContactsRequest, BulkCreateContactsResponse>
{
    private readonly ILogger<BulkCreateContactsEndpoint> _logger;

    public BulkCreateContactsEndpoint(ILogger<BulkCreateContactsEndpoint> logger)
    {
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/bulk/contacts");
        Policies("WriteAccess");
        Options(x => x
            .WithTags("Bulk Operations")
            .WithSummary("Bulk create contacts")
            .WithDescription("Creates multiple contacts in a single operation. Currently returns 501 Not Implemented.")
            .Produces<BulkCreateContactsResponse>(201, "application/json")
            .Produces<BulkCreateContactsResponse>(400, "application/json")
            .Produces<BulkCreateContactsResponse>(501, "application/json")
            .Produces<BulkCreateContactsResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(BulkCreateContactsRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Bulk contact creation attempted with {Count} contacts", req.Contacts?.Count ?? 0);

            // Validate request
            if (req.Contacts == null || req.Contacts.Count == 0)
            {
                var validationResponse = new BulkCreateContactsResponse
                {
                    Success = false,
                    Message = "Request must contain at least one contact.",
                    CreatedCount = 0,
                    ErrorCount = 0
                };

                await SendAsync(validationResponse, 400, ct);
                return;
            }

            var response = new BulkCreateContactsResponse
            {
                Success = false,
                Message = "Bulk contact creation is not implemented. Repository is currently read-only.",
                CreatedCount = 0,
                ErrorCount = 0
            };

            _logger.LogWarning("Bulk contact creation attempted but not implemented. Request contained {Count} contacts", req.Contacts.Count);

            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during bulk contact creation attempt");

            var response = new BulkCreateContactsResponse
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
