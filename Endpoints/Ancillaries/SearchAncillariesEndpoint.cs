using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Endpoints.Ancillaries;

/// <summary>
/// Request model for searching ancillary users.
/// </summary>
public class SearchAncillariesRequest
{
    /// <summary>
    /// Name to search for.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Entity type to search for.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// State to search for.
    /// </summary>
    public string? State { get; set; }
}

/// <summary>
/// Endpoint to search for ancillary users based on various criteria.
/// </summary>
public class SearchAncillariesEndpoint : Endpoint<SearchAncillariesRequest, List<AncillaryUser>>
{
    private readonly IAncillaryUserRepository _repository;
    private readonly ILogger<SearchAncillariesEndpoint> _logger;

    public SearchAncillariesEndpoint(IAncillaryUserRepository repository, ILogger<SearchAncillariesEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/search/ancillaries");
        AllowAnonymous();
        Options(x => x
            .WithTags("Ancillaries")
            .WithSummary("Search ancillary users")
            .WithDescription("Search for ancillary users by name, entity type, or state")
            .Produces<List<AncillaryUser>>(200, "application/json")
            .Produces(StatusCodes.Status500InternalServerError, typeof(ProblemDetails)));
    }

    public override async Task HandleAsync(SearchAncillariesRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Searching ancillary users with criteria - Name: {Name}, EntityType: {EntityType}, State: {State}",
                req.Name ?? "N/A", 
                req.EntityType ?? "N/A", 
                req.State ?? "N/A");
            
            var ancillaries = await _repository.SearchAsync(
                req.Name, 
                req.EntityType, 
                req.State, 
                ct);
            
            _logger.LogInformation("Search returned {Count} ancillary users", ancillaries.Count);
            
            await SendAsync(ancillaries, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching ancillary users");
            
            await SendAsync(new List<AncillaryUser>(), 500, ct);
        }
    }
}
