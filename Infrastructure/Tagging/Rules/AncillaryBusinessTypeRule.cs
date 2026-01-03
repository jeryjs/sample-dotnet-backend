using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Tagging.Rules;

/// <summary>
/// Tags ancillary entities based on their entity subtype (Home Health, Hospice, etc.).
/// </summary>
public sealed class AncillaryBusinessTypeRule : TaggingRuleBase
{
    public override string Name => "AncillaryBusinessType";
    public override string Version => "1.0";
    public override string Description => "Classifies ancillary entities by business type/subtype";
    public override int Priority => 10;

    private static readonly Dictionary<string, string> SubtypeMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Home Health Agency"] = "home-health",
        ["Hospice"] = "hospice",
        ["Physiotherapy Groups"] = "physiotherapy",
        ["Physical Therapy"] = "physiotherapy",
        ["Sleep Study"] = "sleep-study",
        ["Medical Center"] = "medical-center",
        ["Hospitals"] = "hospital",
        ["Housecall"] = "housecall",
        ["Skilled Nursing Facility"] = "snf",
        ["SNF"] = "snf",
        ["Assisted Living"] = "assisted-living",
        ["Memory Care"] = "memory-care",
        ["Rehabilitation"] = "rehabilitation",
        ["DME"] = "durable-medical-equipment",
        ["Durable Medical Equipment"] = "durable-medical-equipment",
        ["Lab"] = "laboratory",
        ["Laboratory"] = "laboratory",
        ["Imaging"] = "imaging",
        ["Radiology"] = "radiology"
    };

    public override bool AppliesTo(TaggingContext context) =>
        context.Entity is AncillaryUser;

    protected override Task<TaggingResult> EvaluateInternalAsync(
        TaggingContext context,
        CancellationToken cancellationToken)
    {
        var ancillary = (AncillaryUser)context.Entity;
        var tags = new List<backend_api.Domain.Common.Tag>();
        var diagnostics = new List<string>();

        // Tag by entity subtype
        if (!string.IsNullOrWhiteSpace(ancillary.EntitySubtype))
        {
            var subtype = ancillary.EntitySubtype.Trim();

            // Check for exact match first
            if (SubtypeMapping.TryGetValue(subtype, out var tagName))
            {
                tags.Add(CreateTag(tagName, "business", confidence: 1.0,
                    metadata: new Dictionary<string, string>
                    {
                        ["originalSubtype"] = subtype,
                        ["category"] = "ancillary-type"
                    }));
                diagnostics.Add($"Classified as '{tagName}' from subtype '{subtype}'");
            }
            else
            {
                // Try partial match for flexible classification
                foreach (var (key, value) in SubtypeMapping)
                {
                    if (subtype.Contains(key, StringComparison.OrdinalIgnoreCase))
                    {
                        tags.Add(CreateTag(value, "business", confidence: 0.85,
                            metadata: new Dictionary<string, string>
                            {
                                ["originalSubtype"] = subtype,
                                ["category"] = "ancillary-type",
                                ["matchType"] = "partial"
                            }));
                        diagnostics.Add($"Classified as '{value}' from partial match in '{subtype}'");
                        break;
                    }
                }
            }

            // Add generic ancillary-service tag
            tags.Add(CreateTag("ancillary-service", "business", confidence: 1.0,
                metadata: new Dictionary<string, string>
                {
                    ["subtype"] = subtype
                }));
        }

        // Tag by entity type
        if (!string.IsNullOrWhiteSpace(ancillary.EntityType))
        {
            var entityType = ancillary.EntityType.ToLowerInvariant();
            tags.Add(CreateTag($"entity-type-{entityType}", "business", confidence: 1.0));
            diagnostics.Add($"Tagged with entity type: {entityType}");
        }

        // Check for clinical services
        if (!string.IsNullOrWhiteSpace(ancillary.ClinicalServices))
        {
            tags.Add(CreateTag("provides-clinical-services", "business", confidence: 1.0));
            diagnostics.Add("Entity provides clinical services");
        }

        if (ancillary.ServicesArray?.Any() == true)
        {
            tags.Add(CreateTag("multi-service", "business", confidence: 1.0,
                metadata: new Dictionary<string, string>
                {
                    ["serviceCount"] = ancillary.ServicesArray.Count.ToString()
                }));
            diagnostics.Add($"Entity offers {ancillary.ServicesArray.Count} service(s)");
        }

        return Task.FromResult(TaggingResult.Successful(tags, new[] { Name }, diagnostics));
    }
}
