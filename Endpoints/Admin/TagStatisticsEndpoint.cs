using FastEndpoints;
using BackendApi.Infrastructure.Tagging;

namespace BackendApi.Endpoints.Admin;

/// <summary>
/// Endpoint to get tag statistics across all collections.
/// </summary>
public class TagStatisticsEndpoint : EndpointWithoutRequest<TagStatistics>
{
    private readonly ITaggingService _taggingService;

    public TagStatisticsEndpoint(ITaggingService taggingService)
    {
        _taggingService = taggingService;
    }

    public override void Configure()
    {
        Get("/admin/tags/statistics");
        Policies("DefaultAccess");
        Options(x => x
            .WithTags("Admin", "Tags")
            .WithSummary("Get tag usage statistics")
            .WithDescription("Returns statistics about tag usage across all collections."));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var stats = await _taggingService.GetTagStatisticsAsync(ct);
        await SendAsync(stats, cancellation: ct);
    }
}
