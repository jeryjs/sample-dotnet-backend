using FastEndpoints;
using BackendApi.Infrastructure.Tagging;

namespace BackendApi.Endpoints.Admin;

/// <summary>
/// Endpoint to get available tagging rules and their metadata.
/// </summary>
public class TagRulesEndpoint : EndpointWithoutRequest<TagRulesResponse>
{
    private readonly ITaggingService _taggingService;

    public TagRulesEndpoint(ITaggingService taggingService)
    {
        _taggingService = taggingService;
    }

    public override void Configure()
    {
        Get("/admin/tags/rules");
        Policies("DefaultAccess");
        Options(x => x
            .WithTags("Admin", "Tags")
            .WithSummary("Get available tagging rules")
            .WithDescription("Returns all configured tagging rules with their metadata and status."));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var rules = _taggingService.GetAvailableRules();

        var response = new TagRulesResponse
        {
            TotalRules = rules.Count,
            EnabledRules = rules.Count(r => r.IsEnabled),
            Rules = rules.Select(r => new TagRuleInfo
            {
                Name = r.Name,
                Version = r.Version,
                Description = r.Description,
                Priority = r.Priority,
                IsEnabled = r.IsEnabled
            }).OrderBy(r => r.Priority).ToList()
        };

        await SendAsync(response, cancellation: ct);
    }
}

public class TagRulesResponse
{
    public int TotalRules { get; init; }
    public int EnabledRules { get; init; }
    public List<TagRuleInfo> Rules { get; init; } = new();
}

public class TagRuleInfo
{
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Priority { get; init; }
    public bool IsEnabled { get; init; }
}
