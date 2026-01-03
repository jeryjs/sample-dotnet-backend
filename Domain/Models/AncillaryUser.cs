using System.Text.Json.Serialization;

namespace backend_api.Domain.Models;

/// <summary>
/// Represents an ancillary user entity such as home health agencies and other service providers.
/// </summary>
public record AncillaryUser
{
    /// <summary>
    /// Gets the unique identifier for the ancillary user.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the entity WAV identifier.
    /// </summary>
    [JsonPropertyName("entityWavId")]
    public required string EntityWavId { get; init; }

    /// <summary>
    /// Gets the name of the ancillary entity.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the type of entity (e.g., "ANCILLIARY").
    /// </summary>
    [JsonPropertyName("entityType")]
    public required string EntityType { get; init; }

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
    [JsonPropertyName("entityNpiNumber")]
    public string? EntityNpiNumber { get; init; }

    /// <summary>
    /// Gets the clinical services provided.
    /// </summary>
    [JsonPropertyName("clinicalServices")]
    public string? ClinicalServices { get; init; }

    /// <summary>
    /// Gets the services provided as a string.
    /// </summary>
    [JsonPropertyName("services")]
    public string? Services { get; init; }

    /// <summary>
    /// Gets the services provided as an array.
    /// </summary>
    [JsonPropertyName("servicesArray")]
    public IReadOnlyCollection<string>? ServicesArray { get; init; }

    /// <summary>
    /// Gets the type of address (e.g., "PRIMARY").
    /// </summary>
    [JsonPropertyName("addressType")]
    public string? AddressType { get; init; }

    /// <summary>
    /// Gets the state where the ancillary entity is located.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    /// <summary>
    /// Gets the city where the ancillary entity is located.
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; init; }

    /// <summary>
    /// Gets the zip code of the ancillary entity.
    /// </summary>
    [JsonPropertyName("zipcode")]
    public string? Zipcode { get; init; }

    /// <summary>
    /// Gets the latitude coordinate of the ancillary entity location.
    /// </summary>
    [JsonPropertyName("lat")]
    public string? Lat { get; init; }

    /// <summary>
    /// Gets the longitude coordinate of the ancillary entity location.
    /// </summary>
    [JsonPropertyName("lon")]
    public string? Lon { get; init; }

    /// <summary>
    /// Gets the email address of the ancillary entity.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>
    /// Gets the phone number of the ancillary entity.
    /// </summary>
    [JsonPropertyName("phoneNo")]
    public string? PhoneNo { get; init; }

    /// <summary>
    /// Gets the collection of entities associated with this ancillary user.
    /// </summary>
    [JsonPropertyName("e_AssociatedEntitys")]
    public IReadOnlyCollection<AssociatedEntity> AssociatedEntities { get; init; } = Array.Empty<AssociatedEntity>();

    /// <summary>
    /// Gets the collection of tags for classification, security, and compliance.
    /// Tags are used for access control, data governance, and automated workflows.
    /// </summary>
    [JsonPropertyName("tags")]
    public IReadOnlyCollection<backend_api.Domain.Common.Tag> Tags { get; init; } = Array.Empty<backend_api.Domain.Common.Tag>();
}
