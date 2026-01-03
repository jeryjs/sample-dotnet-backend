using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Tagging.Rules;

/// <summary>
/// Tags entities with ownership and responsibility information for access control and routing.
/// </summary>
public sealed class OwnershipRule : TaggingRuleBase
{
    public override string Name => "Ownership";
    public override string Version => "1.0";
    public override string Description => "Tags entities with owner and team information for access control";
    public override int Priority => 20;

    public override bool AppliesTo(TaggingContext context) =>
        context.Entity is Patient or ContactUser or AncillaryUser;

    protected override Task<TaggingResult> EvaluateInternalAsync(
        TaggingContext context,
        CancellationToken cancellationToken)
    {
        var tags = new List<backend_api.Domain.Common.Tag>();
        var diagnostics = new List<string>();

        string? owner = context.Entity switch
        {
            ContactUser c => c.ContactOwner,
            _ => null
        };

        string? createdBy = context.Entity switch
        {
            Patient p => p.CreatedBy,
            _ => null
        };

        // Tag by owner
        if (!string.IsNullOrWhiteSpace(owner))
        {
            var ownerEmail = owner.Trim().ToLowerInvariant();

            // Extract team/organization from email domain
            if (ownerEmail.Contains('@'))
            {
                var parts = ownerEmail.Split('@');
                if (parts.Length == 2)
                {
                    var username = parts[0];
                    var domain = parts[1];

                    // Add owner-specific tag
                    tags.Add(CreateTag("owner", "access", confidence: 1.0,
                        value: ownerEmail,
                        metadata: new Dictionary<string, string>
                        {
                            ["username"] = username,
                            ["domain"] = domain
                        }));
                    diagnostics.Add($"Owner: {ownerEmail}");

                    // Add team/org tag based on domain
                    if (domain == "doctoralliance.com")
                    {
                        tags.Add(CreateTag("team-doctoralliance", "access", confidence: 1.0));
                        diagnostics.Add("Team: Doctor Alliance");

                        // Add individual team member tags for routing
                        tags.Add(CreateTag($"assigned-to-{username}", "access", confidence: 1.0));
                        diagnostics.Add($"Assigned to: {username}");
                    }
                }
            }
        }

        // Tag by creator
        if (!string.IsNullOrWhiteSpace(createdBy))
        {
            tags.Add(CreateTag("creator", "access", confidence: 1.0,
                value: createdBy,
                metadata: new Dictionary<string, string>
                {
                    ["createdBy"] = createdBy
                }));
            diagnostics.Add($"Created by: {createdBy}");
        }

        // Tag operation context
        if (!string.IsNullOrWhiteSpace(context.PerformedBy))
        {
            tags.Add(CreateTag("last-modified-by", "access", confidence: 1.0,
                value: context.PerformedBy));
            diagnostics.Add($"Last modified by: {context.PerformedBy}");
        }

        return Task.FromResult(TaggingResult.Successful(tags, new[] { Name }, diagnostics));
    }
}
