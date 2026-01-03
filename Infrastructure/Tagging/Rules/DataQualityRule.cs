using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Tagging.Rules;

/// <summary>
/// Tags entities with data quality indicators based on completeness and placeholder detection.
/// </summary>
public sealed class DataQualityRule : TaggingRuleBase
{
    public override string Name => "DataQuality";
    public override string Version => "1.0";
    public override string Description => "Evaluates and tags data quality issues like incomplete data, placeholders, and test data";
    public override int Priority => 50;

    private static readonly HashSet<string> PlaceholderEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "a@a.com",
        "test@test.com",
        "dummy@dummy.com",
        "example@example.com",
        "a@agmail.com",
        "noemail@example.com"
    };

    private static readonly HashSet<string> PlaceholderPhones = new()
    {
        "0000000000",
        "1111111111",
        "9999999999",
        "+10000000000",
        "+11111111111"
    };

    public override bool AppliesTo(TaggingContext context) =>
        context.Entity is Patient or ContactUser or AncillaryUser;

    protected override Task<TaggingResult> EvaluateInternalAsync(
        TaggingContext context,
        CancellationToken cancellationToken)
    {
        var tags = new List<backend_api.Domain.Common.Tag>();
        var diagnostics = new List<string>();
        var issues = new List<string>();

        // Check for placeholder/test email
        var email = context.Entity switch
        {
            ContactUser contactEntity => contactEntity.Email,
            AncillaryUser ancillaryEntity => ancillaryEntity.Email,
            _ => null
        };

        if (email != null && PlaceholderEmails.Contains(email))
        {
            issues.Add("placeholder-email");
            diagnostics.Add($"Detected placeholder email: {email}");
        }

        // Check for placeholder phone
        var phone = context.Entity switch
        {
            ContactUser contactEntity => contactEntity.PhoneNo,
            AncillaryUser ancillaryEntity => ancillaryEntity.PhoneNo,
            _ => null
        };

        if (phone != null)
        {
            var normalizedPhone = new string(phone.Where(char.IsDigit).ToArray());
            if (PlaceholderPhones.Contains(normalizedPhone) || PlaceholderPhones.Contains(phone))
            {
                issues.Add("placeholder-phone");
                diagnostics.Add($"Detected placeholder phone: {phone}");
            }
        }

        // Check for placeholder NPI
        if (context.Entity is AncillaryUser ancillary)
        {
            if (ancillary.EntityNpiNumber == "0000000000")
            {
                issues.Add("placeholder-npi");
                diagnostics.Add("Detected placeholder NPI: 0000000000");
            }

            // Check for missing critical business fields
            if (string.IsNullOrWhiteSpace(ancillary.EntitySubtype))
            {
                issues.Add("missing-subtype");
                diagnostics.Add("Missing entity subtype");
            }

            if (string.IsNullOrWhiteSpace(ancillary.State) &&
                string.IsNullOrWhiteSpace(ancillary.City) &&
                string.IsNullOrWhiteSpace(ancillary.Zipcode))
            {
                issues.Add("missing-location");
                diagnostics.Add("Missing all location information");
            }
        }

        // Check for incomplete contact information
        if (context.Entity is ContactUser contact)
        {
            var hasValidEmail = IsValidEmail(contact.Email) && !PlaceholderEmails.Contains(contact.Email);
            var phoneDigits = contact.PhoneNo?.Where(char.IsDigit).ToArray();
            var phoneString = phoneDigits != null ? new string(phoneDigits) : string.Empty;
            var hasValidPhone = IsValidPhone(contact.PhoneNo) && !PlaceholderPhones.Contains(phoneString);

            if (!hasValidEmail && !hasValidPhone)
            {
                issues.Add("no-valid-contact-method");
                diagnostics.Add("No valid contact method (email or phone)");
            }

            if (string.IsNullOrWhiteSpace(contact.FirstName) || string.IsNullOrWhiteSpace(contact.LastName))
            {
                issues.Add("incomplete-name");
                diagnostics.Add("Missing first or last name");
            }
        }

        // Check for test/dummy patterns in names
        if (context.Entity is ContactUser cont)
        {
            var fullName = $"{cont.FirstName} {cont.LastName}".ToLowerInvariant();
            if (fullName.Contains("test") || fullName.Contains("dummy") || fullName.Contains("sample"))
            {
                issues.Add("test-name-pattern");
                diagnostics.Add($"Detected test name pattern: {fullName}");
            }
        }

        if (context.Entity is AncillaryUser anc)
        {
            var name = anc.Name?.ToLowerInvariant() ?? "";
            if (name.Contains("test") || name.Contains("dummy") || name.Contains("sample"))
            {
                issues.Add("test-name-pattern");
                diagnostics.Add($"Detected test name pattern: {name}");
            }
        }

        // Apply quality tags based on issues found
        if (issues.Count > 0)
        {
            tags.Add(CreateTag("suspect", "quality", confidence: 1.0,
                metadata: new Dictionary<string, string>
                {
                    ["issues"] = string.Join(",", issues),
                    ["issueCount"] = issues.Count.ToString()
                }));
            diagnostics.Add($"Data quality issues detected: {string.Join(", ", issues)}");

            // Add specific issue tags
            foreach (var issue in issues)
            {
                tags.Add(CreateTag(issue, "quality", confidence: 1.0));
            }
        }
        else
        {
            tags.Add(CreateTag("verified", "quality", confidence: 0.8));
            diagnostics.Add("No obvious data quality issues detected");
        }

        return Task.FromResult(TaggingResult.Successful(tags, new[] { Name }, diagnostics));
    }
}
