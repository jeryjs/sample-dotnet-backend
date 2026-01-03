using System.Text.Json.Serialization;

namespace backend_api.Domain.Models;

/// <summary>
/// Represents a canonical tag definition in the tag catalog.
/// This serves as the source of truth for all valid tags that can be applied across the system.
/// </summary>
public sealed record TagDefinition
{
    /// <summary>
    /// Gets the unique identifier for this tag definition.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the namespace for logical grouping.
    /// </summary>
    [JsonPropertyName("namespace")]
    public required string Namespace { get; init; }

    /// <summary>
    /// Gets the tag name/key.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the human-readable display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the detailed description explaining when and why to use this tag.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>
    /// Gets the category for organizational purposes.
    /// Examples: "Data Classification", "Access Control", "Compliance", "Business Intelligence".
    /// </summary>
    [JsonPropertyName("category")]
    public required string Category { get; init; }

    /// <summary>
    /// Gets the sensitivity level indicator for this tag.
    /// True indicates this tag marks sensitive/restricted data requiring special access controls.
    /// </summary>
    [JsonPropertyName("isSensitive")]
    public bool IsSensitive { get; init; }

    /// <summary>
    /// Gets the collection of roles/groups allowed to view data with this tag.
    /// Empty collection means no special restrictions beyond authentication.
    /// </summary>
    [JsonPropertyName("allowedRoles")]
    public IReadOnlyCollection<string> AllowedRoles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the collection of roles/groups allowed to apply/remove this tag.
    /// </summary>
    [JsonPropertyName("allowedTaggers")]
    public IReadOnlyCollection<string> AllowedTaggers { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets a value indicating whether this tag is deprecated and should not be applied to new entities.
    /// </summary>
    [JsonPropertyName("isDeprecated")]
    public bool IsDeprecated { get; init; }

    /// <summary>
    /// Gets the replacement tag identifier if this tag is deprecated.
    /// Format: "namespace:name"
    /// </summary>
    [JsonPropertyName("replacedBy")]
    public string? ReplacedBy { get; init; }

    /// <summary>
    /// Gets a value indicating whether this tag is managed by automated systems (rules/ML).
    /// True = auto-managed, false = manual only.
    /// </summary>
    [JsonPropertyName("isAutomatic")]
    public bool IsAutomatic { get; init; }

    /// <summary>
    /// Gets a value indicating whether this tag is mutable (can be removed/changed by users).
    /// System-critical tags (e.g., PHI, compliance) may be immutable.
    /// </summary>
    [JsonPropertyName("isMutable")]
    public bool IsMutable { get; init; } = true;

    /// <summary>
    /// Gets the validation regex pattern if this tag accepts a value parameter.
    /// Null indicates no value parameter allowed.
    /// </summary>
    [JsonPropertyName("valuePattern")]
    public string? ValuePattern { get; init; }

    /// <summary>
    /// Gets example values for documentation and validation.
    /// </summary>
    [JsonPropertyName("exampleValues")]
    public IReadOnlyCollection<string>? ExampleValues { get; init; }

    /// <summary>
    /// Gets the icon identifier for UI display (material icon name, emoji, etc.).
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    /// <summary>
    /// Gets the color hex code for UI display.
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; init; }

    /// <summary>
    /// Gets the retention policy in days if this tag implies data retention requirements.
    /// Null indicates no specific retention policy.
    /// </summary>
    [JsonPropertyName("retentionDays")]
    public int? RetentionDays { get; init; }

    /// <summary>
    /// Gets related tag identifiers (format: "namespace:name").
    /// Used for tag hierarchies, alternatives, and recommendations.
    /// </summary>
    [JsonPropertyName("relatedTags")]
    public IReadOnlyCollection<string> RelatedTags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets documentation/help URL for this tag.
    /// </summary>
    [JsonPropertyName("documentationUrl")]
    public string? DocumentationUrl { get; init; }

    /// <summary>
    /// Gets the user or system that created this tag definition.
    /// </summary>
    [JsonPropertyName("createdBy")]
    public required string CreatedBy { get; init; }

    /// <summary>
    /// Gets the timestamp when this definition was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the user or system that last updated this definition.
    /// </summary>
    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; init; }

    /// <summary>
    /// Gets the timestamp when this definition was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Gets the version number for tracking definition changes.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    /// <summary>
    /// Gets additional metadata for extensibility.
    /// </summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Creates a formatted tag identifier.
    /// </summary>
    public string ToIdentifier() => $"{Namespace}:{Name}";

    /// <summary>
    /// Checks if a user role is allowed to view data with this tag.
    /// </summary>
    public bool IsRoleAllowed(string role) =>
        !IsSensitive || AllowedRoles.Count == 0 || AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if a user role is allowed to apply/manage this tag.
    /// </summary>
    public bool CanRoleTag(string role) =>
        AllowedTaggers.Count == 0 || AllowedTaggers.Contains(role, StringComparer.OrdinalIgnoreCase);
}
