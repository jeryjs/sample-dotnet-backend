using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Contacts;

/// <summary>
/// Request model for creating a contact user.
/// </summary>
public class CreateContactRequest
{
    /// <summary>
    /// The contact WAV identifier.
    /// </summary>
    public required string ContactWavId { get; set; }

    /// <summary>
    /// The first name of the contact user.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// The last name of the contact user.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// The email address of the contact user.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// The job title of the contact user (optional).
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// The persona type of the contact user (optional).
    /// </summary>
    public string? PersonaType { get; set; }

    /// <summary>
    /// The lifecycle stage of the contact (optional).
    /// </summary>
    public string? ContactLifecycleStage { get; set; }

    /// <summary>
    /// The state where the contact user is located (optional).
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// The city where the contact user is located (optional).
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// The phone number of the contact user (optional).
    /// </summary>
    public string? PhoneNo { get; set; }

    /// <summary>
    /// The email of the contact owner (optional).
    /// </summary>
    public string? ContactOwner { get; set; }

    /// <summary>
    /// Indicates whether the contact user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Collection of associated entities (optional).
    /// </summary>
    public List<AssociatedEntity>? AssociatedEntities { get; set; }
}

/// <summary>
/// Response model for contact user creation.
/// </summary>
public class CreateContactResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }
}

/// <summary>
/// Endpoint to create a new contact user.
/// </summary>
public class CreateContactEndpoint : Endpoint<CreateContactRequest, CreateContactResponse>
{
    private readonly ILogger<CreateContactEndpoint> _logger;

    public CreateContactEndpoint(ILogger<CreateContactEndpoint> logger)
    {
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/contacts");
        Policies("WriteAccess");
        Options(x => x
            .WithTags("Contacts")
            .WithSummary("Create a new contact user")
            .WithDescription("Creates a new contact user in the system. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<CreateContactResponse>(201, "application/json")
            .Produces<CreateContactResponse>(400, "application/json")
            .Produces<CreateContactResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(CreateContactRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to create new contact user with email: {Email}", req.Email);
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new CreateContactResponse
            {
                Success = false,
                Message = "Contact user creation not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Contact user creation attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating contact user");
            
            var response = new CreateContactResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
