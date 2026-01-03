using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Tagging.Rules;

/// <summary>
/// Tags ancillary user entities with PII sensitivity marker.
/// </summary>
public sealed class AncillaryPiiRule : TaggingRuleBase
{
    public override string Name => "AncillaryPII";
    public override string Version => "1.0";
    public override string Description => "Marks ancillary records containing Personally Identifiable Information";
    public override int Priority => 2;

    public override bool AppliesTo(TaggingContext context) =>
        context.Entity is AncillaryUser;

    protected override Task<TaggingResult> EvaluateInternalAsync(
        TaggingContext context,
        CancellationToken cancellationToken)
    {
        var ancillary = (AncillaryUser)context.Entity;
        var tags = new List<backend_api.Domain.Common.Tag>();
        var diagnostics = new List<string>();
        var piiFields = new List<string>();

        // Check for PII/business identifiable fields
        if (IsValidValue(ancillary.Name))
        {
            piiFields.Add("name");
        }

        if (IsValidEmail(ancillary.Email))
        {
            piiFields.Add("email");
        }

        if (IsValidPhone(ancillary.PhoneNo))
        {
            piiFields.Add("phone");
        }

        if (IsValidValue(ancillary.State))
        {
            piiFields.Add("state");
        }

        if (IsValidValue(ancillary.City))
        {
            piiFields.Add("city");
        }

        if (IsValidNpi(ancillary.EntityNpiNumber))
        {
            piiFields.Add("npi");
            tags.Add(CreateTag("has-npi", "business", confidence: 1.0,
                metadata: new Dictionary<string, string>
                {
                    ["npi"] = ancillary.EntityNpiNumber!
                }));
            diagnostics.Add($"Found valid NPI: {ancillary.EntityNpiNumber}");
        }

        // Only tag if valid identifiable information found
        if (piiFields.Count > 0)
        {
            tags.Add(CreateTag("PII", "sensitivity", confidence: 1.0,
                metadata: new Dictionary<string, string>
                {
                    ["fields"] = string.Join(",", piiFields),
                    ["fieldCount"] = piiFields.Count.ToString(),
                    ["type"] = "ancillary-information"
                }));
            diagnostics.Add($"Found PII fields: {string.Join(", ", piiFields)}");

            // Add ancillary-data business tag
            tags.Add(CreateTag("ancillary-data", "business", confidence: 1.0));
            diagnostics.Add("Tagged as ancillary business data");
        }
        else
        {
            diagnostics.Add("No valid PII fields found (may be placeholder data)");
        }

        return Task.FromResult(TaggingResult.Successful(tags, new[] { Name }, diagnostics));
    }
}
