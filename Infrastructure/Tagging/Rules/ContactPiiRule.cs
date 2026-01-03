using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Tagging.Rules;

/// <summary>
/// Tags contact user entities with PII sensitivity marker when they contain personal information.
/// </summary>
public sealed class ContactPiiRule : TaggingRuleBase
{
    public override string Name => "ContactPII";
    public override string Version => "1.0";
    public override string Description => "Marks contact records containing Personally Identifiable Information";
    public override int Priority => 2;

    public override bool AppliesTo(TaggingContext context) =>
        context.Entity is ContactUser;

    protected override Task<TaggingResult> EvaluateInternalAsync(
        TaggingContext context,
        CancellationToken cancellationToken)
    {
        var contact = (ContactUser)context.Entity;
        var tags = new List<backend_api.Domain.Common.Tag>();
        var diagnostics = new List<string>();
        var piiFields = new List<string>();

        // Check for PII fields
        if (IsValidValue(contact.FirstName))
        {
            piiFields.Add("firstName");
        }

        if (IsValidValue(contact.LastName))
        {
            piiFields.Add("lastName");
        }

        if (IsValidEmail(contact.Email))
        {
            piiFields.Add("email");
        }

        if (IsValidPhone(contact.PhoneNo))
        {
            piiFields.Add("phone");
        }

        if (IsValidValue(contact.State))
        {
            piiFields.Add("state");
        }

        if (IsValidValue(contact.City))
        {
            piiFields.Add("city");
        }

        // Only tag if valid PII found (not test/placeholder data)
        if (piiFields.Count > 0)
        {
            tags.Add(CreateTag("PII", "sensitivity", confidence: 1.0,
                metadata: new Dictionary<string, string>
                {
                    ["fields"] = string.Join(",", piiFields),
                    ["fieldCount"] = piiFields.Count.ToString(),
                    ["type"] = "contact-information"
                }));
            diagnostics.Add($"Found PII fields: {string.Join(", ", piiFields)}");

            // Add contact-data business tag
            tags.Add(CreateTag("contact-data", "business", confidence: 1.0));
            diagnostics.Add("Tagged as contact business data");
        }
        else
        {
            diagnostics.Add("No valid PII fields found (may be placeholder data)");
        }

        return Task.FromResult(TaggingResult.Successful(tags, new[] { Name }, diagnostics));
    }
}
