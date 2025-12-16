using FastEndpoints;
using backend_api.Domain.Models;
using BackendApi.Infrastructure.Repositories;

namespace BackendApi.Endpoints.Stats;

/// <summary>
/// Response DTO for contact statistics.
/// </summary>
public class ContactStatsResponse
{
    /// <summary>
    /// Gets the total number of contacts.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the breakdown of contacts by lifecycle stage.
    /// </summary>
    public Dictionary<string, int> ByLifecycleStage { get; init; } = new();

    /// <summary>
    /// Gets the breakdown of contacts by persona type.
    /// </summary>
    public Dictionary<string, int> ByPersonaType { get; init; } = new();

    /// <summary>
    /// Gets the breakdown of contacts by owner.
    /// </summary>
    public Dictionary<string, int> ByOwner { get; init; } = new();
}

/// <summary>
/// Endpoint to retrieve contact statistics.
/// </summary>
public class GetContactStatsEndpoint : EndpointWithoutRequest<ContactStatsResponse>
{
    private readonly IContactUserRepository _repository;
    private readonly ILogger<GetContactStatsEndpoint> _logger;

    public GetContactStatsEndpoint(IContactUserRepository repository, ILogger<GetContactStatsEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/stats/contacts");
        AllowAnonymous();
        Options(x => x
            .WithTags("Stats")
            .WithSummary("Get contact statistics")
            .WithDescription("Retrieves aggregated statistics for contacts including total count and breakdown by lifecycle stage, persona type, and owner")
            .Produces<ContactStatsResponse>(200, "application/json")
            .Produces(500, "application/json"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching contact statistics");
            
            var contacts = await _repository.GetAllAsync(ct);
            
            var stats = new ContactStatsResponse
            {
                TotalCount = contacts.Count,
                ByLifecycleStage = contacts
                    .GroupBy(c => c.ContactLifecycleStage ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count()),
                ByPersonaType = contacts
                    .GroupBy(c => c.PersonaType ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count()),
                ByOwner = contacts
                    .GroupBy(c => c.ContactOwner ?? "Unassigned")
                    .ToDictionary(g => g.Key, g => g.Count())
            };
            
            _logger.LogInformation("Successfully retrieved contact statistics: Total={Total}, LifecycleStages={Stages}, PersonaTypes={Types}, Owners={Owners}", 
                stats.TotalCount, stats.ByLifecycleStage.Count, stats.ByPersonaType.Count, stats.ByOwner.Count);
            
            await SendAsync(stats, cancellation: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching contact statistics");
            
            await SendAsync(new ContactStatsResponse(), 500, ct);
        }
    }
}
