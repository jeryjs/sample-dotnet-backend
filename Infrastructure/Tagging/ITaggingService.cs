using backend_api.Domain.Common;
using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Tagging;

/// <summary>
/// Service for applying tags to entities based on configurable rules.
/// Provides both synchronous tagging (for create/update) and batch backfill operations.
/// </summary>
public interface ITaggingService
{
    /// <summary>
    /// Evaluates an entity and returns the tags that should be applied (dry-run mode).
    /// Does not persist changes - used for preview and validation.
    /// </summary>
    Task<TaggingResult> EvaluateAsync(
        object entity,
        string operation = "evaluate",
        string? performedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies tags to an entity and returns the updated entity with tags.
    /// For Patient, ContactUser, or AncillaryUser entities.
    /// </summary>
    Task<T> ApplyTagsAsync<T>(
        T entity,
        string operation = "apply",
        string? performedBy = null,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Backfills tags for all entities in a collection.
    /// Returns summary statistics and can operate in dry-run mode.
    /// </summary>
    Task<BackfillResult> BackfillCollectionAsync(
        string collectionName,
        bool dryRun = true,
        int batchSize = 100,
        string? performedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about tag usage across all entities.
    /// </summary>
    Task<TagStatistics> GetTagStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates tags against the tag catalog.
    /// Returns validation errors for unknown or deprecated tags.
    /// </summary>
    Task<ValidationResult> ValidateTagsAsync(
        IEnumerable<Tag> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available tagging rules with their metadata.
    /// </summary>
    IReadOnlyCollection<ITaggingRule> GetAvailableRules();

    /// <summary>
    /// Removes expired tags from an entity.
    /// </summary>
    Task<T> CleanupExpiredTagsAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : class;
}

/// <summary>
/// Result of a backfill operation containing statistics and samples.
/// </summary>
public sealed record BackfillResult
{
    public required string CollectionName { get; init; }
    public int TotalEntities { get; init; }
    public int ProcessedEntities { get; init; }
    public int SuccessfullyTagged { get; init; }
    public int FailedEntities { get; init; }
    public int TotalTagsAdded { get; init; }
    public int TotalTagsRemoved { get; init; }
    public Dictionary<string, int> TagsByNamespace { get; init; } = new();
    public Dictionary<string, int> TagsByName { get; init; } = new();
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public TimeSpan Duration { get; init; }
    public bool IsDryRun { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

/// <summary>
/// Statistics about tag usage across the system.
/// </summary>
public sealed record TagStatistics
{
    public int TotalTags { get; init; }
    public int TotalEntitiesWithTags { get; init; }
    public Dictionary<string, int> TagCountByNamespace { get; init; } = new();
    public Dictionary<string, int> TagCountByName { get; init; } = new();
    public Dictionary<string, int> TagCountBySource { get; init; } = new();
    public List<TagUsage> TopTags { get; init; } = new();
    public List<Tag> ExpiredTags { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

public sealed record TagUsage
{
    public required string TagIdentifier { get; init; }
    public required string Namespace { get; init; }
    public required string Name { get; init; }
    public int UsageCount { get; init; }
    public double Percentage { get; init; }
}

/// <summary>
/// Result of tag validation.
/// </summary>
public sealed record ValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public List<Tag> UnknownTags { get; init; } = new();
    public List<Tag> DeprecatedTags { get; init; } = new();
}
