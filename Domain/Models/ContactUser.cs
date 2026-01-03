using System.Text.Json.Serialization;

namespace backend_api.Domain.Models;

/// <summary>
/// Represents a contact user within the system with associated entity relationships.
/// </summary>
public record ContactUser
{
    /// <summary>
    /// Gets the unique identifier for the contact user.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the contact WAV identifier.
    /// </summary>
    [JsonPropertyName("contactWavId")]
    public required string ContactWavId { get; init; }

    /// <summary>
    /// Gets the collection of entities associated with this contact user.
    /// </summary>
    [JsonPropertyName("associatedEntities")]
    public IReadOnlyCollection<AssociatedEntity> AssociatedEntities { get; init; } = Array.Empty<AssociatedEntity>();

    /// <summary>
    /// Gets the first name of the contact user.
    /// </summary>
    [JsonPropertyName("firstName")]
    public required string FirstName { get; init; }

    /// <summary>
    /// Gets the last name of the contact user.
    /// </summary>
    [JsonPropertyName("lastName")]
    public required string LastName { get; init; }

    /// <summary>
    /// Gets the job title of the contact user.
    /// </summary>
    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; init; }

    /// <summary>
    /// Gets the persona type of the contact user (e.g., "Neutral").
    /// </summary>
    [JsonPropertyName("personaType")]
    public string? PersonaType { get; init; }

    /// <summary>
    /// Gets the lifecycle stage of the contact (e.g., "User").
    /// </summary>
    [JsonPropertyName("contactLifecycleStage")]
    public string? ContactLifecycleStage { get; init; }

    /// <summary>
    /// Gets the state where the contact user is located.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    /// <summary>
    /// Gets the city where the contact user is located.
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; init; }

    /// <summary>
    /// Gets the email address of the contact user.
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// Gets the phone number of the contact user.
    /// </summary>
    [JsonPropertyName("phoneNo")]
    public string? PhoneNo { get; init; }

    /// <summary>
    /// Gets the email of the contact owner.
    /// </summary>
    [JsonPropertyName("contactOwner")]
    public string? ContactOwner { get; init; }

    /// <summary>
    /// Gets a value indicating whether the contact user is active.
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets the collection of tags for classification, security, and compliance.
    /// Tags are used for access control, data governance, and automated workflows.
    /// </summary>
    [JsonPropertyName("tags")]
    public IReadOnlyCollection<backend_api.Domain.Common.Tag> Tags { get; init; } = Array.Empty<backend_api.Domain.Common.Tag>();
}
