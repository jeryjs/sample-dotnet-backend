using System.Text.Json.Serialization;

namespace backend_api.Domain.Common;

/// <summary>
/// Represents a structured, versioned tag for classification, security, governance, and compliance.
/// Tags are immutable and support multi-namespace organization with confidence scoring and lifecycle management.
/// </summary>
public sealed record Tag
{
    /// <summary>
    /// Gets the name/key of the tag (e.g., "PHI", "home-health", "embargoed").
    /// This is the primary identifier within a namespace.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the namespace for logical grouping and access control.
    /// Common namespaces: "sensitivity", "business", "security", "quality", "retention", "access", "compliance".
    /// </summary>
    [JsonPropertyName("namespace")]
    public required string Namespace { get; init; }

    /// <summary>
    /// Gets the source system or rule that generated this tag.
    /// Examples: "import", "rule:v1", "rule:v2", "ml:model_20260103", "purview:scan", "manual".
    /// Enables audit trails and tag provenance tracking.
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// Gets the confidence score (0.0 to 1.0) indicating certainty of tag assignment.
    /// 1.0 = deterministic/manual, 0.0-0.99 = probabilistic/ML-based.
    /// Null indicates not applicable (deterministic rules).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double? Confidence { get; init; }

    /// <summary>
    /// Gets the optional value/metadata associated with the tag for parameterized tags.
    /// Examples: for "access:owner" tag, value might be "john@example.com".
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    /// <summary>
    /// Gets the user or system that created/applied this tag.
    /// Format: "user:email" or "system:service-name".
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Gets the timestamp when the tag was created/applied.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the optional expiration timestamp for time-bound tags.
    /// Null indicates no expiration. Used for temporary access grants, embargoes, etc.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Gets additional metadata as key-value pairs for extensibility.
    /// Can store rule version, context, reasoning, etc.
    /// </summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Gets a value indicating whether this tag is active/valid.
    /// Allows soft deletion or deprecation without removing historical tags.
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Creates a formatted tag identifier string for logging and display.
    /// Format: "namespace:name" or "namespace:name=value"
    /// </summary>
    public string ToIdentifier() =>
        string.IsNullOrEmpty(Value)
            ? $"{Namespace}:{Name}"
            : $"{Namespace}:{Name}={Value}";

    /// <summary>
    /// Checks if the tag has expired based on current UTC time.
    /// </summary>
    public bool IsExpired() =>
        ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt.Value;

    /// <summary>
    /// Checks if the tag is currently valid (active and not expired).
    /// </summary>
    public bool IsValid() =>
        IsActive && !IsExpired();

    /// <summary>
    /// Factory method to create a simple tag with required fields.
    /// </summary>
    public static Tag Create(
        string name,
        string @namespace,
        string source,
        double? confidence = null,
        string? value = null,
        string? createdBy = null,
        DateTime? expiresAt = null,
        Dictionary<string, string>? metadata = null)
    {
        return new Tag
        {
            Name = name,
            Namespace = @namespace,
            Source = source,
            Confidence = confidence,
            Value = value,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            Metadata = metadata,
            IsActive = true
        };
    }

    /// <summary>
    /// Creates an inactive/deprecated version of this tag.
    /// </summary>
    public Tag Deactivate() => this with { IsActive = false };

    /// <summary>
    /// Creates a new tag with updated expiration.
    /// </summary>
    public Tag WithExpiration(DateTime expiresAt) => this with { ExpiresAt = expiresAt };

    /// <summary>
    /// Creates a new tag with additional metadata.
    /// </summary>
    public Tag WithMetadata(string key, string value)
    {
        var newMetadata = Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                          ?? new Dictionary<string, string>();
        newMetadata[key] = value;
        return this with { Metadata = newMetadata };
    }
}
