using System.Text.Json.Serialization;

namespace backend_api.Domain.Models;

/// <summary>
/// Represents a patient entity with agency information and care details.
/// </summary>
public record Patient
{
    /// <summary>
    /// Gets the unique identifier for the patient.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the timestamp when the patient record was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; init; }

    /// <summary>
    /// Gets the identifier of the user who created the patient record.
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Gets a value indicating whether the patient is billable.
    /// </summary>
    [JsonPropertyName("isBillable")]
    public bool? IsBillable { get; init; }

    /// <summary>
    /// Gets a value indicating whether the patient is PG billable.
    /// </summary>
    [JsonPropertyName("isPgBillable")]
    public bool? IsPgBillable { get; init; }

    /// <summary>
    /// Gets a value indicating whether the patient is eligible.
    /// </summary>
    [JsonPropertyName("isEligible")]
    public bool? IsEligible { get; init; }

    /// <summary>
    /// Gets a value indicating whether the patient is PG eligible.
    /// </summary>
    [JsonPropertyName("isPgEligible")]
    public bool? IsPgEligible { get; init; }

    /// <summary>
    /// Gets the agency information associated with the patient.
    /// </summary>
    [JsonPropertyName("agencyInfo")]
    public required AgencyInfo AgencyInfo { get; init; }
}
