using FastEndpoints;
using BackendApi.Infrastructure.Tagging;

namespace BackendApi.Endpoints.Admin;

/// <summary>
/// Endpoint to backfill tags for all entities in a collection.
/// Supports dry-run mode for preview before committing changes.
/// </summary>
public class TagBackfillEndpoint : Endpoint<TagBackfillRequest, BackfillResult>
{
    private readonly ITaggingService _taggingService;
    private readonly ILogger<TagBackfillEndpoint> _logger;

    public TagBackfillEndpoint(
        ITaggingService taggingService,
        ILogger<TagBackfillEndpoint> logger)
    {
        _taggingService = taggingService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/admin/tags/backfill");
        Policies("AdminOnly"); // Require admin role
        Options(x => x
            .WithTags("Admin", "Tags")
            .WithSummary("Backfill tags for a collection")
            .WithDescription("Applies tagging rules to all entities in a collection. Use dryRun=true to preview changes."));
    }

    public override async Task HandleAsync(TagBackfillRequest req, CancellationToken ct)
    {
        _logger.LogInformation(
            "Tag backfill requested for collection '{Collection}' (DryRun={DryRun}, User={User})",
            req.CollectionName,
            req.DryRun,
            User.Identity?.Name ?? "Unknown");

        try
        {
            var result = await _taggingService.BackfillCollectionAsync(
                req.CollectionName,
                req.DryRun,
                req.BatchSize,
                User.Identity?.Name,
                ct);

            await SendAsync(result, cancellation: ct);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid collection name: {Collection}", req.CollectionName);
            await SendErrorsAsync(400, ct);
            AddError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tag backfill failed for collection '{Collection}'", req.CollectionName);
            await SendErrorsAsync(500, ct);
            AddError($"Backfill failed: {ex.Message}");
        }
    }
}

public class TagBackfillRequest
{
    /// <summary>
    /// Collection name to backfill (patients, contacts, ancillaries).
    /// </summary>
    public required string CollectionName { get; init; }

    /// <summary>
    /// If true, only preview changes without persisting.
    /// </summary>
    public bool DryRun { get; init; } = true;

    /// <summary>
    /// Number of entities to process in each batch.
    /// </summary>
    public int BatchSize { get; init; } = 100;
}
