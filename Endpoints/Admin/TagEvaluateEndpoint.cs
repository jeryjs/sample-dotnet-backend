using FastEndpoints;
using BackendApi.Infrastructure.Tagging;
using backend_api.Domain.Common;

namespace BackendApi.Endpoints.Admin;

/// <summary>
/// Endpoint to evaluate tags for a single entity without persisting (dry-run evaluation).
/// </summary>
public class TagEvaluateEndpoint : Endpoint<TagEvaluateRequest, TagEvaluateResponse>
{
    private readonly ITaggingService _taggingService;

    public TagEvaluateEndpoint(ITaggingService taggingService)
    {
        _taggingService = taggingService;
    }

    public override void Configure()
    {
        Post("/admin/tags/evaluate");
        Policies("DefaultAccess");
        Options(x => x
            .WithTags("Admin", "Tags")
            .WithSummary("Evaluate tags for an entity")
            .WithDescription("Evaluates tagging rules for an entity without persisting. Returns predicted tags and diagnostics."));
    }

    public override async Task HandleAsync(TagEvaluateRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _taggingService.EvaluateAsync(
                req.Entity,
                "evaluate",
                User.Identity?.Name,
                ct);

            var response = new TagEvaluateResponse
            {
                Success = result.Success,
                Tags = result.Tags.Select(t => new TagDto
                {
                    Namespace = t.Namespace,
                    Name = t.Name,
                    Source = t.Source,
                    Confidence = t.Confidence,
                    Value = t.Value,
                    Metadata = t.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                }).ToList(),
                ExecutedRules = result.ExecutedRules.ToList(),
                Diagnostics = result.Diagnostics.ToList(),
                Warnings = result.Warnings.ToList(),
                Errors = result.Errors.ToList()
            };

            await SendAsync(response, cancellation: ct);
        }
        catch (Exception ex)
        {
            await SendErrorsAsync(500, ct);
            AddError($"Evaluation failed: {ex.Message}");
        }
    }
}

public class TagEvaluateRequest
{
    /// <summary>
    /// The entity object to evaluate (Patient, ContactUser, or AncillaryUser).
    /// </summary>
    public required object Entity { get; init; }
}

public class TagEvaluateResponse
{
    public bool Success { get; init; }
    public List<TagDto> Tags { get; init; } = new();
    public List<string> ExecutedRules { get; init; } = new();
    public List<string> Diagnostics { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public List<string> Errors { get; init; } = new();
}

public class TagDto
{
    public string Namespace { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public double? Confidence { get; init; }
    public string? Value { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}
