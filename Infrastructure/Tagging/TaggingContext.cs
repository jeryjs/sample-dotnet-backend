using backend_api.Domain.Common;

namespace BackendApi.Infrastructure.Tagging;

/// <summary>
/// Context object containing entity data and metadata for tag evaluation.
/// </summary>
public sealed record TaggingContext
{
    /// <summary>
    /// Gets the entity being evaluated (Patient, ContactUser, AncillaryUser, etc.).
    /// </summary>
    public required object Entity { get; init; }

    /// <summary>
    /// Gets the fully qualified type name of the entity.
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Gets the entity identifier if available.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Gets the operation context (create, update, import, backfill).
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// Gets the user or system performing the operation.
    /// </summary>
    public string? PerformedBy { get; init; }

    /// <summary>
    /// Gets existing tags already applied to the entity.
    /// Used for incremental tagging and conflict detection.
    /// </summary>
    public IReadOnlyCollection<Tag> ExistingTags { get; init; } = Array.Empty<Tag>();

    /// <summary>
    /// Gets additional context metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets the timestamp of this tagging operation.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
