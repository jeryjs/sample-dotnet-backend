using System.Text.Json.Serialization;

namespace backend_api.Domain.Models;

/// <summary>
/// Represents an episode diagnosis for a patient.
/// </summary>
public record EpisodeDiagnosis
{
    /// <summary>
    /// Gets the unique identifier for the diagnosis.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// Gets the start of care date for this diagnosis (format: MM/dd/yyyy).
    /// </summary>
    [JsonPropertyName("startOfCare")]
    public string? StartOfCare { get; init; }

    /// <summary>
    /// Gets the first diagnosis code and description.
    /// </summary>
    [JsonPropertyName("firstDiagnosis")]
    public string? FirstDiagnosis { get; init; }

    /// <summary>
    /// Gets the second diagnosis code and description.
    /// </summary>
    [JsonPropertyName("secondDiagnosis")]
    public string? SecondDiagnosis { get; init; }
}
