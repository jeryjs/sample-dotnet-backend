using System.Text.Json.Serialization;

namespace backend_api.Domain.Models;

/// <summary>
/// Represents an entity associated with a contact user, such as an agency or organization.
/// </summary>
public record AssociatedEntity
{
    /// <summary>
    /// Gets the unique identifier for the associated entity.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the type of entity (e.g., "ANCILLIARY").
    /// </summary>
    [JsonPropertyName("entityType")]
    public required string EntityType { get; init; }

    /// <summary>
    /// Gets the name of the entity.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the job title associated with this entity relationship.
    /// </summary>
    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; init; }

    /// <summary>
    /// Gets the subtype of the entity (e.g., "Home Health Agency").
    /// </summary>
    [JsonPropertyName("entitySubtype")]
    public string? EntitySubtype { get; init; }

    /// <summary>
    /// Gets the lifecycle stage of the entity (e.g., "Freemium").
    /// </summary>
    [JsonPropertyName("lifecycleStage")]
    public string? LifecycleStage { get; init; }

    /// <summary>
    /// Gets the National Provider Identifier (NPI) number.
    /// </summary>
    [JsonPropertyName("npiNumber")]
    public string? NpiNumber { get; init; }
}
