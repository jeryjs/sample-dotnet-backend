using System.Security.Claims;
using backend_api.Domain.Common;
using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Security;

/// <summary>
/// Service for filtering entities based on user's tag access permissions.
/// Prevents unauthorized access to sensitive data at the repository level.
/// </summary>
public interface ITagAccessFilter
{
    /// <summary>
    /// Filters a collection of entities to only include those the user is authorized to access.
    /// </summary>
    IEnumerable<T> FilterByTagAccess<T>(IEnumerable<T> entities, ClaimsPrincipal user) where T : class;

    /// <summary>
    /// Checks if a user can access a specific entity based on its tags.
    /// </summary>
    bool CanAccess<T>(T entity, ClaimsPrincipal user) where T : class;

    /// <summary>
    /// Gets a sanitized version of an entity with sensitive fields redacted if user lacks access.
    /// </summary>
    T SanitizeEntity<T>(T entity, ClaimsPrincipal user) where T : class;
}

/// <summary>
/// Implementation of tag-based access filtering.
/// </summary>
public class TagAccessFilter : ITagAccessFilter
{
    private readonly ILogger<TagAccessFilter> _logger;

    public TagAccessFilter(ILogger<TagAccessFilter> logger)
    {
        _logger = logger;
    }

    public IEnumerable<T> FilterByTagAccess<T>(IEnumerable<T> entities, ClaimsPrincipal user) where T : class
    {
        return entities.Where(entity => CanAccess(entity, user));
    }

    public bool CanAccess<T>(T entity, ClaimsPrincipal user) where T : class
    {
        // Admin can access everything
        if (user.IsInRole("Admin"))
            return true;

        var tags = GetEntityTags(entity);

        // No tags = no restrictions
        if (!tags.Any())
            return true;

        // Check each sensitive tag
        foreach (var tag in tags.Where(t => t.Namespace == "sensitivity"))
        {
            if (!HasSensitivityAccess(user, tag))
            {
                _logger.LogDebug(
                    "User {User} denied access to entity with tag {Tag}",
                    user.Identity?.Name ?? "Anonymous",
                    tag.ToIdentifier());
                return false;
            }
        }

        // Check ownership/access tags
        var accessTags = tags.Where(t => t.Namespace == "access").ToList();
        if (accessTags.Any())
        {
            // Must have access to at least one access tag
            if (!accessTags.Any(tag => HasAccessPermission(user, tag)))
            {
                _logger.LogDebug(
                    "User {User} denied access - no matching access tags",
                    user.Identity?.Name ?? "Anonymous");
                return false;
            }
        }

        return true;
    }

    public T SanitizeEntity<T>(T entity, ClaimsPrincipal user) where T : class
    {
        // This is a simplified implementation
        // A full implementation would create redacted copies of entities
        // For now, we just return the entity if accessible, null otherwise
        return CanAccess(entity, user) ? entity : null!;
    }

    private IReadOnlyCollection<Tag> GetEntityTags(object entity)
    {
        return entity switch
        {
            Patient p => p.Tags,
            ContactUser c => c.Tags,
            AncillaryUser a => a.Tags,
            _ => Array.Empty<Tag>()
        };
    }

    private bool HasSensitivityAccess(ClaimsPrincipal user, Tag tag)
    {
        var requiredRoles = tag.Name.ToLowerInvariant() switch
        {
            "phi" => new[] { "Admin", "Clinician", "PHI-Reader" },
            "pii" => new[] { "Admin", "User", "PII-Reader" },
            "clinical-data" => new[] { "Admin", "Clinician" },
            "diagnosis-data" => new[] { "Admin", "Clinician" },
            _ => Array.Empty<string>()
        };

        return requiredRoles.Length == 0 || requiredRoles.Any(role => user.IsInRole(role));
    }

    private bool HasAccessPermission(ClaimsPrincipal user, Tag tag)
    {
        // Admin can access everything
        if (user.IsInRole("Admin"))
            return true;

        // Check ownership
        if (tag.Name == "owner" && !string.IsNullOrEmpty(tag.Value))
        {
            var userEmail = user.Identity?.Name?.ToLowerInvariant();
            var ownerEmail = tag.Value.ToLowerInvariant();

            if (userEmail == ownerEmail)
                return true;

            // Same domain access
            if (userEmail != null && ownerEmail.Contains('@') && userEmail.Contains('@'))
            {
                var userDomain = userEmail.Split('@')[1];
                var ownerDomain = ownerEmail.Split('@')[1];
                if (userDomain == ownerDomain)
                    return true;
            }
        }

        // Check team assignment
        if (tag.Name.StartsWith("team-") || tag.Name.StartsWith("assigned-to-"))
        {
            var userEmail = user.Identity?.Name?.ToLowerInvariant();
            if (userEmail != null && tag.Name.Contains(userEmail.Split('@')[0]))
                return true;
        }

        return false;
    }
}
