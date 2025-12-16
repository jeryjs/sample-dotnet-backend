using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Endpoints.Ancillaries;

/// <summary>
/// Request model for getting an ancillary user by WAV ID.
/// </summary>
public class GetAncillaryByWavIdRequest
{
    /// <summary>
    /// The WAV identifier of the ancillary user.
    /// </summary>
    public string WavId { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to retrieve an ancillary user by their WAV ID.
/// </summary>
public class GetAncillaryByWavIdEndpoint : Endpoint<GetAncillaryByWavIdRequest, AncillaryUser>
{
    private readonly IAncillaryUserRepository _repository;
    private readonly ILogger<GetAncillaryByWavIdEndpoint> _logger;

    public GetAncillaryByWavIdEndpoint(IAncillaryUserRepository repository, ILogger<GetAncillaryByWavIdEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/ancillaries/wavid/{wavId}");
        Policies("ReadAccess");
        Options(x => x
            .WithTags("Ancillaries")
            .WithSummary("Get ancillary user by WAV ID")
            .WithDescription("Retrieves a specific ancillary user by their WAV identifier")
            .Produces<AncillaryUser>(200, "application/json")
            .Produces(StatusCodes.Status404NotFound, typeof(ProblemDetails))
            .Produces(StatusCodes.Status500InternalServerError, typeof(ProblemDetails)));
    }

    public override async Task HandleAsync(GetAncillaryByWavIdRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching ancillary user with WAV ID: {WavId}", req.WavId);
            
            var ancillary = await _repository.GetByWavIdAsync(req.WavId, ct);
            
            if (ancillary == null)
            {
                _logger.LogWarning("Ancillary user with WAV ID {WavId} not found", req.WavId);
                await SendNotFoundAsync(ct);
                return;
            }
            
            _logger.LogInformation("Successfully retrieved ancillary user with WAV ID: {WavId}", req.WavId);
            
            await SendAsync(ancillary, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching ancillary user with WAV ID: {WavId}", req.WavId);
            
            await SendAsync(null!, 500, ct);
        }
    }
}
