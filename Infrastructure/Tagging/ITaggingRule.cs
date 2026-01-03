namespace BackendApi.Infrastructure.Tagging;

/// <summary>
/// Interface for tagging rules that evaluate entities and produce tags.
/// Rules should be stateless, idempotent, and fast.
/// </summary>
public interface ITaggingRule
{
    /// <summary>
    /// Gets the unique name/identifier for this rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of this rule for tracking changes.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the description of what this rule does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the priority/order for rule execution (lower executes first).
    /// Allows dependencies between rules.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this rule is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Determines if this rule applies to the given context.
    /// Fast pre-check before expensive evaluation.
    /// </summary>
    bool AppliesTo(TaggingContext context);

    /// <summary>
    /// Evaluates the context and produces tagging result.
    /// </summary>
    Task<TaggingResult> EvaluateAsync(TaggingContext context, CancellationToken cancellationToken = default);
}
