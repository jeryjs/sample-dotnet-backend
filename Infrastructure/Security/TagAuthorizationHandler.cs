using Microsoft.AspNetCore.Authorization;
using backend_api.Domain.Common;

namespace BackendApi.Infrastructure.Security;

/// <summary>
/// Authorization requirement that checks if a user has permission to access entities with specific tags.
/// </summary>
public class TagAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the tag namespace that must be checked.
    /// </summary>
    public string? Namespace { get; }

    /// <summary>
    /// Gets the tag name that must be checked.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets a value indicating whether ANY of the allowed roles is sufficient (OR logic).
    /// False requires ALL allowed roles (AND logic).
    /// </summary>
    public bool RequireAnyRole { get; }

    public TagAuthorizationRequirement(
        string? @namespace = null,
        string? name = null,
        bool requireAnyRole = true)
    {
        Namespace = @namespace;
        Name = name;
        RequireAnyRole = requireAnyRole;
    }
}

/// <summary>
/// Handler that enforces tag-based access control using tag catalog definitions.
/// </summary>
public class TagAuthorizationHandler : AuthorizationHandler<TagAuthorizationRequirement, IEnumerable<Tag>>
{
    private readonly ILogger<TagAuthorizationHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TagAuthorizationHandler(
        ILogger<TagAuthorizationHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TagAuthorizationRequirement requirement,
        IEnumerable<Tag> tags)
    {
        var user = context.User;
        var tagList = tags.ToList();

        // If no tags, allow access (no sensitive markers)
        if (!tagList.Any())
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Filter tags by requirement criteria
        var relevantTags = tagList.Where(t =>
        {
            if (requirement.Namespace != null && t.Namespace != requirement.Namespace)
                return false;
            if (requirement.Name != null && t.Name != requirement.Name)
                return false;
            return true;
        }).ToList();

        // If no relevant tags match the requirement, allow access
        if (!relevantTags.Any())
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check user roles against tag requirements
        foreach (var tag in relevantTags)
        {
            // Check for sensitivity namespace tags (PHI, PII, etc.)
            if (tag.Namespace == "sensitivity")
            {
                if (!HasRequiredSensitivityAccess(user, tag))
                {
                    _logger.LogWarning(
                        "User {User} denied access to {TagIdentifier} - insufficient permissions",
                        user.Identity?.Name ?? "Anonymous",
                        tag.ToIdentifier());
                    
                    LogAccessDenial(user, tag);
                    return Task.CompletedTask; // Deny
                }
            }

            // Check for access namespace tags (ownership, team assignment)
            if (tag.Namespace == "access")
            {
                if (!HasRequiredAccessPermission(user, tag))
                {
                    _logger.LogWarning(
                        "User {User} denied access to {TagIdentifier} - not authorized for this resource",
                        user.Identity?.Name ?? "Anonymous",
                        tag.ToIdentifier());
                    
                    LogAccessDenial(user, tag);
                    return Task.CompletedTask; // Deny
                }
            }
        }

        // All checks passed
        context.Succeed(requirement);
        return Task.CompletedTask;
    }

    private bool HasRequiredSensitivityAccess(System.Security.Claims.ClaimsPrincipal user, Tag tag)
    {
        // Map tag names to required roles
        var roleRequirements = tag.Name.ToLowerInvariant() switch
        {
            "phi" => new[] { "Admin", "Clinician", "PHI-Reader" },
            "pii" => new[] { "Admin", "User", "PII-Reader" },
            "clinical-data" => new[] { "Admin", "Clinician" },
            "diagnosis-data" => new[] { "Admin", "Clinician" },
            _ => Array.Empty<string>()
        };

        // If no specific requirements, allow
        if (roleRequirements.Length == 0)
            return true;

        // Check if user has ANY of the required roles
        return roleRequirements.Any(role => user.IsInRole(role));
    }

    private bool HasRequiredAccessPermission(System.Security.Claims.ClaimsPrincipal user, Tag tag)
    {
        // Admin can access everything
        if (user.IsInRole("Admin"))
            return true;

        // Check ownership tags
        if (tag.Name == "owner" && !string.IsNullOrEmpty(tag.Value))
        {
            var userEmail = user.Identity?.Name?.ToLowerInvariant();
            var ownerEmail = tag.Value.ToLowerInvariant();
            
            // Allow if user is the owner
            if (userEmail == ownerEmail)
                return true;

            // Check if user is in the same organization/domain
            if (userEmail != null && ownerEmail.Contains('@') && userEmail.Contains('@'))
            {
                var userDomain = userEmail.Split('@')[1];
                var ownerDomain = ownerEmail.Split('@')[1];
                
                // Allow same domain access (can be made configurable)
                if (userDomain == ownerDomain)
                    return true;
            }
        }

        // Check team assignment tags
        if (tag.Name.StartsWith("team-") || tag.Name.StartsWith("assigned-to-"))
        {
            // Extract team/user from tag and check against user claims
            // This is a simplified check - real implementation would check against
            // team membership claims or database
            var userEmail = user.Identity?.Name?.ToLowerInvariant();
            if (userEmail != null && tag.Name.Contains(userEmail.Split('@')[0]))
                return true;
        }

        // Default deny for unmatched access tags
        return false;
    }

    private void LogAccessDenial(System.Security.Claims.ClaimsPrincipal user, Tag tag)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var requestPath = httpContext?.Request.Path.ToString() ?? "Unknown";

        _logger.LogWarning(
            "Access denied: User={User}, Tag={Tag}, IP={IP}, Path={Path}, Timestamp={Timestamp}",
            user.Identity?.Name ?? "Anonymous",
            tag.ToIdentifier(),
            ipAddress,
            requestPath,
            DateTime.UtcNow);
    }
}
