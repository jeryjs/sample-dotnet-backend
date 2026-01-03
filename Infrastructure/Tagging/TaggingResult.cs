using backend_api.Domain.Common;

namespace BackendApi.Infrastructure.Tagging;

/// <summary>
/// Result of tag rule evaluation containing tags and diagnostic information.
/// </summary>
public sealed record TaggingResult
{
    /// <summary>
    /// Gets the collection of tags produced by rule evaluation.
    /// </summary>
    public IReadOnlyCollection<Tag> Tags { get; init; } = Array.Empty<Tag>();

    /// <summary>
    /// Gets the collection of tags that should be removed (for update operations).
    /// </summary>
    public IReadOnlyCollection<Tag> TagsToRemove { get; init; } = Array.Empty<Tag>();

    /// <summary>
    /// Gets diagnostic messages produced during evaluation.
    /// </summary>
    public IReadOnlyCollection<string> Diagnostics { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets warnings encountered during evaluation.
    /// </summary>
    public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets errors encountered during evaluation.
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the collection of rule names that were executed.
    /// </summary>
    public IReadOnlyCollection<string> ExecutedRules { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets performance metrics for rule execution.
    /// </summary>
    public IReadOnlyDictionary<string, TimeSpan>? RuleExecutionTimes { get; init; }

    /// <summary>
    /// Gets a value indicating whether the evaluation completed successfully.
    /// </summary>
    public bool Success => Errors.Count == 0;

    /// <summary>
    /// Gets the total number of new tags.
    /// </summary>
    public int NewTagCount => Tags.Count;

    /// <summary>
    /// Gets the total number of tags to be removed.
    /// </summary>
    public int RemovalCount => TagsToRemove.Count;

    /// <summary>
    /// Creates a successful result with tags.
    /// </summary>
    public static TaggingResult Successful(
        IEnumerable<Tag> tags,
        IEnumerable<string>? executedRules = null,
        IEnumerable<string>? diagnostics = null)
    {
        return new TaggingResult
        {
            Tags = tags.ToList(),
            ExecutedRules = executedRules?.ToList() ?? new List<string>(),
            Diagnostics = diagnostics?.ToList() ?? new List<string>()
        };
    }

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    public static TaggingResult Failed(params string[] errors)
    {
        return new TaggingResult
        {
            Errors = errors.ToList()
        };
    }

    /// <summary>
    /// Merges multiple tagging results into a single result.
    /// </summary>
    public static TaggingResult Merge(params TaggingResult[] results)
    {
        return new TaggingResult
        {
            Tags = results.SelectMany(r => r.Tags).ToList(),
            TagsToRemove = results.SelectMany(r => r.TagsToRemove).ToList(),
            Diagnostics = results.SelectMany(r => r.Diagnostics).ToList(),
            Warnings = results.SelectMany(r => r.Warnings).ToList(),
            Errors = results.SelectMany(r => r.Errors).ToList(),
            ExecutedRules = results.SelectMany(r => r.ExecutedRules).Distinct().ToList(),
            RuleExecutionTimes = results
                .Where(r => r.RuleExecutionTimes != null)
                .SelectMany(r => r.RuleExecutionTimes!)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }

    /// <summary>
    /// Creates a copy with additional diagnostics.
    /// </summary>
    public TaggingResult WithDiagnostic(string message)
    {
        return this with
        {
            Diagnostics = Diagnostics.Append(message).ToList()
        };
    }

    /// <summary>
    /// Creates a copy with additional warnings.
    /// </summary>
    public TaggingResult WithWarning(string message)
    {
        return this with
        {
            Warnings = Warnings.Append(message).ToList()
        };
    }

    /// <summary>
    /// Creates a copy with additional errors.
    /// </summary>
    public TaggingResult WithError(string message)
    {
        return this with
        {
            Errors = Errors.Append(message).ToList()
        };
    }
}
