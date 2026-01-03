using backend_api.Domain.Common;
using System.Diagnostics;

namespace BackendApi.Infrastructure.Tagging;

/// <summary>
/// Abstract base class for tagging rules with common functionality.
/// </summary>
public abstract class TaggingRuleBase : ITaggingRule
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract string Version { get; }

    /// <inheritdoc/>
    public abstract string Description { get; }

    /// <inheritdoc/>
    public virtual int Priority => 100;

    /// <inheritdoc/>
    public virtual bool IsEnabled => true;

    /// <inheritdoc/>
    public abstract bool AppliesTo(TaggingContext context);

    /// <inheritdoc/>
    public async Task<TaggingResult> EvaluateAsync(TaggingContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return TaggingResult.Successful(
                Array.Empty<Tag>(),
                new[] { Name },
                new[] { $"Rule '{Name}' is disabled" });
        }

        if (!AppliesTo(context))
        {
            return TaggingResult.Successful(
                Array.Empty<Tag>(),
                new[] { Name },
                new[] { $"Rule '{Name}' does not apply to this context" });
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await EvaluateInternalAsync(context, cancellationToken);
            stopwatch.Stop();

            return result with
            {
                ExecutedRules = result.ExecutedRules.Append(Name).ToList(),
                RuleExecutionTimes = new Dictionary<string, TimeSpan>
                {
                    [Name] = stopwatch.Elapsed
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return TaggingResult.Failed($"Rule '{Name}' failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Internal evaluation logic to be implemented by derived rules.
    /// </summary>
    protected abstract Task<TaggingResult> EvaluateInternalAsync(
        TaggingContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Helper to create a tag with standard source format.
    /// </summary>
    protected Tag CreateTag(
        string name,
        string @namespace,
        double? confidence = null,
        string? value = null,
        string? createdBy = null,
        DateTime? expiresAt = null,
        Dictionary<string, string>? metadata = null)
    {
        var source = $"rule:{Name}:{Version}";
        var effectiveMetadata = metadata ?? new Dictionary<string, string>();
        effectiveMetadata["rule"] = Name;
        effectiveMetadata["version"] = Version;

        return Tag.Create(
            name,
            @namespace,
            source,
            confidence,
            value,
            createdBy,
            expiresAt,
            effectiveMetadata);
    }

    /// <summary>
    /// Helper to check if a string value exists and is not a placeholder/test value.
    /// </summary>
    protected static bool IsValidValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim().ToLowerInvariant();

        // Check for common placeholder/test patterns
        var invalidPatterns = new[]
        {
            "a@a.com",
            "test@test.com",
            "dummy@dummy.com",
            "example@example.com",
            "noemail",
            "no-email",
            "n/a",
            "na",
            "none",
            "null",
            "unknown",
            "0000000000", // Placeholder phone/NPI
            "1111111111",
            "9999999999"
        };

        return !invalidPatterns.Contains(normalized) &&
               !normalized.Contains("test") &&
               !normalized.Contains("dummy") &&
               !normalized.Contains("placeholder");
    }

    /// <summary>
    /// Helper to check if an email appears valid.
    /// </summary>
    protected static bool IsValidEmail(string? email)
    {
        if (!IsValidValue(email))
            return false;

        return email!.Contains('@') &&
               email.Contains('.') &&
               email.Length >= 5;
    }

    /// <summary>
    /// Helper to check if a phone number appears valid.
    /// </summary>
    protected static bool IsValidPhone(string? phone)
    {
        if (!IsValidValue(phone))
            return false;

        // Remove common formatting characters
        var digits = new string(phone!.Where(char.IsDigit).ToArray());
        return digits.Length >= 10 && digits.Length <= 15;
    }

    /// <summary>
    /// Helper to check if NPI number appears valid.
    /// </summary>
    protected static bool IsValidNpi(string? npi)
    {
        if (!IsValidValue(npi))
            return false;

        var digits = new string(npi!.Where(char.IsDigit).ToArray());
        return digits.Length == 10 && digits != "0000000000";
    }

    /// <summary>
    /// Helper to get a property value from an object safely.
    /// </summary>
    protected static object? GetPropertyValue(object obj, string propertyPath)
    {
        var parts = propertyPath.Split('.');
        object? current = obj;

        foreach (var part in parts)
        {
            if (current == null)
                return null;

            var property = current.GetType().GetProperty(part);
            if (property == null)
                return null;

            current = property.GetValue(current);
        }

        return current;
    }

    /// <summary>
    /// Helper to safely get string property value.
    /// </summary>
    protected static string? GetStringProperty(object obj, string propertyPath)
    {
        return GetPropertyValue(obj, propertyPath) as string;
    }
}
