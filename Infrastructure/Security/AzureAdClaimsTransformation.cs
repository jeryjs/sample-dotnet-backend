using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace BackendApi.Infrastructure.Security;

/// <summary>
/// Transforms Azure AD claims by mapping group membership to roles for authorization
/// </summary>
public class AzureAdClaimsTransformation : IClaimsTransformation
{
    private readonly ILogger<AzureAdClaimsTransformation> _logger;
    private readonly IConfiguration _configuration;

    // Map Azure AD group object IDs to role names
    private readonly Dictionary<string, string> _groupToRoleMap;

    public AzureAdClaimsTransformation(
        ILogger<AzureAdClaimsTransformation> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Load group-to-role mappings from configuration
        // Format in appsettings.json:
        // "AzureAd": {
        //   "GroupRoleMappings": {
        //     "admin-group-id": "Admin",
        //     "writer-group-id": "Writer",
        //     "reader-group-id": "Reader"
        //   }
        // }
        _groupToRoleMap = new Dictionary<string, string>();

        var mappings = _configuration.GetSection("AzureAd:GroupRoleMappings");
        foreach (var mapping in mappings.GetChildren())
        {
            _groupToRoleMap[mapping.Key] = mapping.Value ?? mapping.Key;
        }

        // Default mappings if not configured
        if (_groupToRoleMap.Count == 0)
        {
            _logger.LogWarning(
                "No group-to-role mappings configured. Using empty mappings. " +
                "Configure 'AzureAd:GroupRoleMappings' in appsettings.json");
        }
        else
        {
            _logger.LogInformation(
                "Loaded {Count} group-to-role mappings: {Mappings}",
                _groupToRoleMap.Count,
                string.Join(", ", _groupToRoleMap.Select(m => $"{m.Key}→{m.Value}")));
        }
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        try
        {
            // Log user identity for audit trail
            LogUserIdentity(principal);

            // Clone the identity to add new claims
            var clonedIdentity = new ClaimsIdentity(
                identity.Claims,
                identity.AuthenticationType,
                identity.NameClaimType,
                identity.RoleClaimType);

            // Transform Azure AD groups to roles
            var groupClaims = principal.FindAll("groups").ToList();
            var addedRoles = new List<string>();

            foreach (var groupClaim in groupClaims)
            {
                var groupId = groupClaim.Value;

                if (_groupToRoleMap.TryGetValue(groupId, out var roleName))
                {
                    // Add role claim
                    clonedIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                    addedRoles.Add(roleName);

                    _logger.LogDebug(
                        "Mapped group {GroupId} to role {Role} for user {User}",
                        groupId,
                        roleName,
                        GetUserIdentifier(principal));
                }
                else
                {
                    _logger.LogDebug(
                        "Group {GroupId} not mapped to any role for user {User}",
                        groupId,
                        GetUserIdentifier(principal));
                }
            }

            if (addedRoles.Count != 0)
            {
                _logger.LogInformation(
                    "Added roles {Roles} for user {User}",
                    string.Join(", ", addedRoles),
                    GetUserIdentifier(principal));
            }

            // Log all transformed claims for debugging
            LogTransformedClaims(clonedIdentity);

            return Task.FromResult(new ClaimsPrincipal(clonedIdentity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming claims for user {User}",
                GetUserIdentifier(principal));
            return Task.FromResult(principal);
        }
    }

    /// <summary>
    /// Logs user identity information for audit trail
    /// </summary>
    private void LogUserIdentity(ClaimsPrincipal principal)
    {
        var email = principal.FindFirst("preferred_username")?.Value
            ?? principal.FindFirst("email")?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value;

        var name = principal.FindFirst("name")?.Value
            ?? principal.FindFirst(ClaimTypes.Name)?.Value;

        var objectId = principal.FindFirst("oid")?.Value
            ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

        var upn = principal.FindFirst("upn")?.Value
            ?? principal.FindFirst(ClaimTypes.Upn)?.Value;

        _logger.LogInformation(
            "User authenticated - Email: {Email}, Name: {Name}, ObjectId: {ObjectId}, UPN: {UPN}",
            email ?? "N/A",
            name ?? "N/A",
            objectId ?? "N/A",
            upn ?? "N/A");
    }

    /// <summary>
    /// Gets a user identifier for logging purposes
    /// </summary>
    private static string GetUserIdentifier(ClaimsPrincipal principal)
    {
        return principal.FindFirst("preferred_username")?.Value
            ?? principal.FindFirst("email")?.Value
            ?? principal.FindFirst("oid")?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.Identity?.Name
            ?? "Unknown";
    }

    /// <summary>
    /// Logs all transformed claims for debugging
    /// </summary>
    private void LogTransformedClaims(ClaimsIdentity identity)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var claims = identity.Claims
            .Select(c => $"{c.Type}: {c.Value}")
            .ToList();

        _logger.LogDebug(
            "Transformed claims for user {User}: {Claims}",
            identity.Name ?? "Unknown",
            string.Join(", ", claims));
    }
}
