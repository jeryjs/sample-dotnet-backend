using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Contacts;

/// <summary>
/// Request model for getting a contact user by WAV ID.
/// </summary>
public class GetContactByWavIdRequest
{
    /// <summary>
    /// The WAV identifier of the contact user.
    /// </summary>
    public string WavId { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to retrieve a contact user by their WAV ID.
/// </summary>
public class GetContactByWavIdEndpoint : Endpoint<GetContactByWavIdRequest, ContactUser>
{
    private readonly IContactUserRepository _repository;
    private readonly ILogger<GetContactByWavIdEndpoint> _logger;

    public GetContactByWavIdEndpoint(IContactUserRepository repository, ILogger<GetContactByWavIdEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/contacts/wavid/{wavId}");
        AllowAnonymous();
        Options(x => x
            .WithTags("Contacts")
            .WithSummary("Get contact user by WAV ID")
            .WithDescription("Retrieves a specific contact user by their WAV identifier")
            .Produces<ContactUser>(200, "application/json")
            .Produces(404, "application/json")
            .Produces(500, "application/json"));
    }

    public override async Task HandleAsync(GetContactByWavIdRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching contact user with WAV ID: {WavId}", req.WavId);
            
            var contact = await _repository.GetByWavIdAsync(req.WavId, ct);
            
            if (contact == null)
            {
                _logger.LogWarning("Contact user with WAV ID {WavId} not found", req.WavId);
                await SendNotFoundAsync(ct);
                return;
            }
            
            _logger.LogInformation("Successfully retrieved contact user with WAV ID: {WavId}", req.WavId);
            
            await SendAsync(contact, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching contact user with WAV ID: {WavId}", req.WavId);
            
            await SendAsync(null!, 500, ct);
        }
    }
}
