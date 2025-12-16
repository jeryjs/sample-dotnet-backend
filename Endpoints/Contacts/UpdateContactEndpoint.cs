using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Contacts;

/// <summary>
/// Request model for updating a contact user.
/// </summary>
public class UpdateContactRequest
{
    /// <summary>
    /// The unique identifier of the contact user to update.
    /// </summary>
    public Guid Id { get; set; }

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
    public bool IsActive { get; set; }

    /// <summary>
    /// Collection of associated entities (optional).
    /// </summary>
    public List<AssociatedEntity>? AssociatedEntities { get; set; }
}

/// <summary>
/// Response model for contact user update.
/// </summary>
public class UpdateContactResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to update an existing contact user.
/// </summary>
public class UpdateContactEndpoint : Endpoint<UpdateContactRequest, UpdateContactResponse>
{
    private readonly IContactUserRepository _repository;
    private readonly ILogger<UpdateContactEndpoint> _logger;

    public UpdateContactEndpoint(IContactUserRepository repository, ILogger<UpdateContactEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Put("/contacts/{id}");
        Policies("WriteAccess");
        Options(x => x
            .WithTags("Contacts")
            .WithSummary("Update a contact user")
            .WithDescription("Updates an existing contact user with all provided fields. Note: Currently read-only repository - this is a placeholder for future implementation")
            .Produces<UpdateContactResponse>(200, "application/json")
            .Produces<UpdateContactResponse>(404, "application/json")
            .Produces<UpdateContactResponse>(500, "application/json"));
    }

    public override async Task HandleAsync(UpdateContactRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting to update contact user with ID: {Id}", req.Id);
            
            // Check if contact exists
            var existingContact = await _repository.GetByIdAsync(req.Id, ct);
            
            if (existingContact == null)
            {
                _logger.LogWarning("Contact user with ID {Id} not found", req.Id);
                
                var notFoundResponse = new UpdateContactResponse
                {
                    Success = false,
                    Message = $"Contact user with ID {req.Id} not found"
                };
                
                await SendAsync(notFoundResponse, 404, ct);
                return;
            }
            
            // Note: The current repository is read-only (in-memory from JSON)
            // This endpoint is a placeholder for when write operations are implemented
            
            var response = new UpdateContactResponse
            {
                Success = false,
                Message = "Contact user update not yet implemented. Repository is currently read-only."
            };
            
            _logger.LogWarning("Contact user update attempted but not yet implemented");
            
            await SendAsync(response, 501, ct); // 501 Not Implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating contact user with ID: {Id}", req.Id);
            
            var response = new UpdateContactResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
            
            await SendAsync(response, 500, ct);
        }
    }
}
