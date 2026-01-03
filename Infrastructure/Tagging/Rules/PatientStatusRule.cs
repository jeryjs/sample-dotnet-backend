using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Tagging.Rules;

/// <summary>
/// Tags patient entities based on their status and clinical indicators.
/// </summary>
public sealed class PatientStatusRule : TaggingRuleBase
{
    public override string Name => "PatientStatus";
    public override string Version => "1.0";
    public override string Description => "Tags patients based on status, eligibility, and billing indicators";
    public override int Priority => 15;

    public override bool AppliesTo(TaggingContext context) =>
        context.Entity is Patient;

    protected override Task<TaggingResult> EvaluateInternalAsync(
        TaggingContext context,
        CancellationToken cancellationToken)
    {
        var patient = (Patient)context.Entity;
        var tags = new List<backend_api.Domain.Common.Tag>();
        var diagnostics = new List<string>();

        // Tag by patient status
        if (!string.IsNullOrWhiteSpace(patient.AgencyInfo?.PatientStatus))
        {
            var status = patient.AgencyInfo.PatientStatus.Trim().ToLowerInvariant();
            tags.Add(CreateTag($"status-{status}", "business", confidence: 1.0,
                metadata: new Dictionary<string, string>
                {
                    ["originalStatus"] = patient.AgencyInfo.PatientStatus
                }));
            diagnostics.Add($"Patient status: {status}");

            // Add active/inactive classification
            if (status.Contains("active"))
            {
                tags.Add(CreateTag("active-patient", "business", confidence: 1.0));
                diagnostics.Add("Patient is active");
            }
            else if (status.Contains("inactive") || status.Contains("discharged"))
            {
                tags.Add(CreateTag("inactive-patient", "business", confidence: 1.0));
                diagnostics.Add("Patient is inactive/discharged");
            }
        }

        // Tag billing indicators
        if (patient.IsBillable == true)
        {
            tags.Add(CreateTag("billable", "business", confidence: 1.0));
            diagnostics.Add("Patient is billable");
        }

        if (patient.IsPgBillable == true)
        {
            tags.Add(CreateTag("pg-billable", "business", confidence: 1.0));
            diagnostics.Add("Patient is PG billable");
        }

        // Tag eligibility indicators
        if (patient.IsEligible == true)
        {
            tags.Add(CreateTag("eligible", "business", confidence: 1.0));
            diagnostics.Add("Patient is eligible");
        }

        if (patient.IsPgEligible == true)
        {
            tags.Add(CreateTag("pg-eligible", "business", confidence: 1.0));
            diagnostics.Add("Patient is PG eligible");
        }

        // Tag by care management complexity
        var careRecords = patient.AgencyInfo?.CareManagement?.Count ?? 0;
        if (careRecords > 0)
        {
            var complexity = careRecords switch
            {
                > 10 => "high-complexity",
                > 5 => "medium-complexity",
                _ => "low-complexity"
            };

            tags.Add(CreateTag(complexity, "business", confidence: 0.9,
                metadata: new Dictionary<string, string>
                {
                    ["careRecordCount"] = careRecords.ToString()
                }));
            diagnostics.Add($"Care complexity: {complexity} ({careRecords} records)");
        }

        // Tag by diagnosis count
        var diagnosisCount = patient.AgencyInfo?.EpisodeDiagnoses?.Count ?? 0;
        if (diagnosisCount > 0)
        {
            tags.Add(CreateTag("has-diagnoses", "business", confidence: 1.0,
                metadata: new Dictionary<string, string>
                {
                    ["diagnosisCount"] = diagnosisCount.ToString()
                }));
            diagnostics.Add($"Has {diagnosisCount} diagnosis/diagnoses");

            if (diagnosisCount >= 3)
            {
                tags.Add(CreateTag("multi-morbidity", "business", confidence: 0.85));
                diagnostics.Add("Multiple diagnoses detected (multi-morbidity indicator)");
            }
        }

        // Tag by start of care date presence
        if (!string.IsNullOrWhiteSpace(patient.AgencyInfo?.StartOfCare))
        {
            tags.Add(CreateTag("has-start-of-care", "business", confidence: 1.0));
            diagnostics.Add($"Start of care date: {patient.AgencyInfo.StartOfCare}");
        }

        return Task.FromResult(TaggingResult.Successful(tags, new[] { Name }, diagnostics));
    }
}
