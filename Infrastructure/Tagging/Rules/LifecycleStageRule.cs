using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Tagging.Rules;

/// <summary>
/// Tags entities based on their lifecycle stage (Freemium, Premium, Untouched, etc.).
/// </summary>
public sealed class LifecycleStageRule : TaggingRuleBase
{
    public override string Name => "LifecycleStage";
    public override string Version => "1.0";
    public override string Description => "Tags entities based on their business lifecycle stage";
    public override int Priority => 15;

    private static readonly Dictionary<string, (string TagName, string Category)> StageMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Freemium"] = ("freemium", "tier"),
        ["Premium"] = ("premium", "tier"),
        ["360 Full"] = ("full-service", "tier"),
        ["Onboarded"] = ("onboarded", "status"),
        ["Engaged"] = ("engaged", "status"),
        ["Common Patients"] = ("shared-patients", "relationship"),
        ["DA Direct"] = ("da-direct", "relationship"),
        ["Untouched"] = ("untouched", "status"),
        ["Targeted"] = ("targeted", "status"),
        ["User"] = ("active-user", "status"),
        ["In Sale Cycle"] = ("in-sales", "status"),
        ["Premium User"] = ("premium-user", "tier")
    };

    public override bool AppliesTo(TaggingContext context) =>
        context.Entity is AncillaryUser or ContactUser;

    protected override Task<TaggingResult> EvaluateInternalAsync(
        TaggingContext context,
        CancellationToken cancellationToken)
    {
        var tags = new List<backend_api.Domain.Common.Tag>();
        var diagnostics = new List<string>();

        string? lifecycleStage = context.Entity switch
        {
            AncillaryUser ancillary => ancillary.LifecycleStage,
            ContactUser contact => contact.ContactLifecycleStage,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(lifecycleStage))
        {
            var stage = lifecycleStage.Trim();

            if (StageMapping.TryGetValue(stage, out var mapping))
            {
                tags.Add(CreateTag(mapping.TagName, "lifecycle", confidence: 1.0,
                    metadata: new Dictionary<string, string>
                    {
                        ["originalStage"] = stage,
                        ["category"] = mapping.Category
                    }));
                diagnostics.Add($"Lifecycle stage: {mapping.TagName} (from '{stage}')");

                // Add engagement level tags
                if (mapping.Category == "status")
                {
                    if (mapping.TagName is "engaged" or "onboarded" or "active-user" or "in-sales")
                    {
                        tags.Add(CreateTag("high-engagement", "business", confidence: 0.9));
                        diagnostics.Add("High engagement level detected");
                    }
                    else if (mapping.TagName == "untouched")
                    {
                        tags.Add(CreateTag("no-engagement", "business", confidence: 1.0));
                        diagnostics.Add("No engagement detected");
                    }
                }

                // Add tier-based access tags
                if (mapping.Category == "tier")
                {
                    tags.Add(CreateTag($"tier-{mapping.TagName}", "access", confidence: 1.0));
                    diagnostics.Add($"Access tier: {mapping.TagName}");
                }
            }
            else
            {
                // Unknown lifecycle stage - tag for review
                tags.Add(CreateTag("unknown-lifecycle", "quality", confidence: 1.0,
                    metadata: new Dictionary<string, string>
                    {
                        ["value"] = stage
                    }));
                diagnostics.Add($"Unknown lifecycle stage: '{stage}'");
            }
        }

        // Check for associated entities relationships
        if (context.Entity is ContactUser contact && contact.AssociatedEntities.Count > 0)
        {
            tags.Add(CreateTag("has-associations", "relationship", confidence: 1.0,
                metadata: new Dictionary<string, string>
                {
                    ["count"] = contact.AssociatedEntities.Count.ToString()
                }));
            diagnostics.Add($"Has {contact.AssociatedEntities.Count} associated entit(ies)");
        }

        if (context.Entity is AncillaryUser ancillaryUser && ancillaryUser.AssociatedEntities.Count > 0)
        {
            tags.Add(CreateTag("has-associations", "relationship", confidence: 1.0,
                metadata: new Dictionary<string, string>
                {
                    ["count"] = ancillaryUser.AssociatedEntities.Count.ToString()
                }));
            diagnostics.Add($"Has {ancillaryUser.AssociatedEntities.Count} associated entit(ies)");
        }

        return Task.FromResult(TaggingResult.Successful(tags, new[] { Name }, diagnostics));
    }
}
