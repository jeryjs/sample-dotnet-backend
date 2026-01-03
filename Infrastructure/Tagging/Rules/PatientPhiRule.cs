using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Tagging.Rules;

/// <summary>
/// Tags patient entities with PHI (Protected Health Information) sensitivity marker.
/// Applies to all Patient entities as they inherently contain PHI.
/// </summary>
public sealed class PatientPhiRule : TaggingRuleBase
{
    public override string Name => "PatientPHI";
    public override string Version => "1.0";
    public override string Description => "Marks patient records as containing Protected Health Information (PHI)";
    public override int Priority => 1; // Execute first as this is fundamental

    public override bool AppliesTo(TaggingContext context) =>
        context.Entity is Patient;

    protected override Task<TaggingResult> EvaluateInternalAsync(
        TaggingContext context,
        CancellationToken cancellationToken)
    {
        var patient = (Patient)context.Entity;
        var tags = new List<backend_api.Domain.Common.Tag>();
        var diagnostics = new List<string>();

        // All patient records contain PHI by definition
        tags.Add(CreateTag("PHI", "sensitivity", confidence: 1.0,
            metadata: new Dictionary<string, string>
            {
                ["reason"] = "Patient records inherently contain Protected Health Information",
                ["regulation"] = "HIPAA",
                ["category"] = "healthcare"
            }));
        diagnostics.Add("Tagged as PHI: Patient record");

        // Check for explicit PII fields
        var agencyInfo = patient.AgencyInfo;
        var hasPiiFields = false;

        if (IsValidValue(agencyInfo?.PatientFName))
        {
            diagnostics.Add($"Found: First Name");
            hasPiiFields = true;
        }

        if (IsValidValue(agencyInfo?.PatientLName))
        {
            diagnostics.Add($"Found: Last Name");
            hasPiiFields = true;
        }

        if (IsValidValue(agencyInfo?.Dob))
        {
            diagnostics.Add($"Found: Date of Birth");
            hasPiiFields = true;
        }

        // Check for clinical information
        if (agencyInfo?.CareManagement?.Any() == true)
        {
            tags.Add(CreateTag("clinical-data", "sensitivity", confidence: 1.0,
                metadata: new Dictionary<string, string>
                {
                    ["recordCount"] = agencyInfo.CareManagement.Count.ToString()
                }));
            diagnostics.Add($"Found: {agencyInfo.CareManagement.Count} care management record(s)");
        }

        if (agencyInfo?.EpisodeDiagnoses?.Any() == true)
        {
            tags.Add(CreateTag("diagnosis-data", "sensitivity", confidence: 1.0,
                metadata: new Dictionary<string, string>
                {
                    ["recordCount"] = agencyInfo.EpisodeDiagnoses.Count.ToString()
                }));
            diagnostics.Add($"Found: {agencyInfo.EpisodeDiagnoses.Count} diagnosis record(s)");
        }

        // Add PII tag if explicit PII fields found
        if (hasPiiFields)
        {
            tags.Add(CreateTag("PII", "sensitivity", confidence: 1.0,
                metadata: new Dictionary<string, string>
                {
                    ["type"] = "patient-identifiers"
                }));
        }

        // Add retention tag for HIPAA compliance (6 years minimum)
        tags.Add(CreateTag("7y", "retention", confidence: 1.0,
            metadata: new Dictionary<string, string>
            {
                ["reason"] = "HIPAA minimum retention requirement",
                ["regulation"] = "HIPAA"
            }));
        diagnostics.Add("Applied 7-year retention policy per HIPAA");

        return Task.FromResult(TaggingResult.Successful(tags, new[] { Name }, diagnostics));
    }
}
