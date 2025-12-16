using System.Text.Json.Serialization;

namespace backend_api.Domain.Models;

/// <summary>
/// Represents a care management record for a patient.
/// </summary>
public record CareManagement
{
    /// <summary>
    /// Gets the type of care management (e.g., CPO).
    /// </summary>
    [JsonPropertyName("careManagementType")]
    public string? CareManagementType { get; init; }
}
