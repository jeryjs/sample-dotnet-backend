using System.Text.Json.Serialization;

namespace backend_api.Domain.Models;

/// <summary>
/// Represents agency-specific information for a patient.
/// </summary>
public record AgencyInfo
{
    /// <summary>
    /// Gets the filter status for the patient.
    /// </summary>
    [JsonPropertyName("filterStatus")]
    public string? FilterStatus { get; init; }

    /// <summary>
    /// Gets the patient's WAV identifier.
    /// </summary>
    [JsonPropertyName("patientWAVId")]
    public string? PatientWAVId { get; init; }

    /// <summary>
    /// Gets the patient's EHR record identifier.
    /// </summary>
    [JsonPropertyName("patientEHRRecId")]
    public string? PatientEHRRecId { get; init; }

    /// <summary>
    /// Gets the patient's first name.
    /// </summary>
    [JsonPropertyName("patientFName")]
    public string? PatientFName { get; init; }

    /// <summary>
    /// Gets the patient's middle name.
    /// </summary>
    [JsonPropertyName("patientMName")]
    public string? PatientMName { get; init; }

    /// <summary>
    /// Gets the patient's last name.
    /// </summary>
    [JsonPropertyName("patientLName")]
    public string? PatientLName { get; init; }

    /// <summary>
    /// Gets the patient's date of birth (format: MM/dd/yyyy).
    /// </summary>
    [JsonPropertyName("dob")]
    public string? Dob { get; init; }

    /// <summary>
    /// Gets the patient's age.
    /// </summary>
    [JsonPropertyName("age")]
    public string? Age { get; init; }

    /// <summary>
    /// Gets the patient's sex.
    /// </summary>
    [JsonPropertyName("patientSex")]
    public string? PatientSex { get; init; }

    /// <summary>
    /// Gets the patient's status (e.g., Active, Inactive).
    /// </summary>
    [JsonPropertyName("patientStatus")]
    public string? PatientStatus { get; init; }

    /// <summary>
    /// Gets the start of care date (format: MM/dd/yyyy).
    /// </summary>
    [JsonPropertyName("startOfCare")]
    public string? StartOfCare { get; init; }

    /// <summary>
    /// Gets the collection of care management records for the patient.
    /// </summary>
    [JsonPropertyName("careManagement")]
    public IReadOnlyList<CareManagement>? CareManagement { get; init; }

    /// <summary>
    /// Gets the collection of episode diagnoses for the patient.
    /// </summary>
    [JsonPropertyName("episodeDiagnoses")]
    public IReadOnlyList<EpisodeDiagnosis>? EpisodeDiagnoses { get; init; }
}
