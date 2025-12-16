using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Ancillaries;

/// <summary>
/// Request model for getting an ancillary user by ID.
/// </summary>
public class GetAncillaryByIdRequest
{
    /// <summary>
    /// The unique identifier of the ancillary user.
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Endpoint to retrieve an ancillary user by their ID.
/// </summary>
public class GetAncillaryByIdEndpoint : Endpoint<GetAncillaryByIdRequest, AncillaryUser>
{
    private readonly IAncillaryUserRepository _repository;
    private readonly ILogger<GetAncillaryByIdEndpoint> _logger;

    public GetAncillaryByIdEndpoint(IAncillaryUserRepository repository, ILogger<GetAncillaryByIdEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/ancillaries/{id}");
        AllowAnonymous();
        Options(x => x
            .WithTags("Ancillaries")
            .WithSummary("Get ancillary user by ID")
            .WithDescription("Retrieves a specific ancillary user by their unique identifier")
            .Produces<AncillaryUser>(200, "application/json")
            .Produces(404, "application/json")
            .Produces(500, "application/json"));
    }

    public override async Task HandleAsync(GetAncillaryByIdRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching ancillary user with ID: {Id}", req.Id);
            
            var ancillary = await _repository.GetByIdAsync(req.Id, ct);
            
            if (ancillary == null)
            {
                _logger.LogWarning("Ancillary user with ID {Id} not found", req.Id);
                await SendNotFoundAsync(ct);
                return;
            }
            
            _logger.LogInformation("Successfully retrieved ancillary user with ID: {Id}", req.Id);
            
            await SendAsync(ancillary, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching ancillary user with ID: {Id}", req.Id);
            
            await SendAsync(null!, 500, ct);
        }
    }
}
