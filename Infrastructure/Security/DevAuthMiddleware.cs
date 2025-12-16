using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BackendApi.Infrastructure.Security;

/// <summary>
/// Simple development-only authentication middleware.
/// When enabled (DEV_AUTH__ENABLED=true), it injects a synthetic ClaimsPrincipal based on environment vars.
/// DO NOT enable in production.
/// </summary>
public class DevAuthMiddleware : IMiddleware
{
    private readonly IConfiguration _config;
    private readonly ILogger<DevAuthMiddleware> _logger;

    public DevAuthMiddleware(IConfiguration config, ILogger<DevAuthMiddleware> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var enabled = _config.GetValue<bool?>("DEV_AUTH__ENABLED") ?? false;
        if (!enabled)
            return next(context);

        var userId = _config["DEV_AUTH__USERID"] ?? "dev-user";
        var userName = _config["DEV_AUTH__USERNAME"] ?? "dev@example.com";
        var displayName = _config["DEV_AUTH__DISPLAYNAME"] ?? "Dev User";
        var roles = _config["DEV_AUTH__ROLES"] ?? "Admin";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim("display_name", displayName),
            new Claim("preferred_username", userName)
        };

        foreach (var role in roles.Split(',').Select(r => r.Trim()).Where(r => !string.IsNullOrEmpty(r)))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "DevAuth");
        var principal = new ClaimsPrincipal(identity);

        context.User = principal;

        _logger.LogDebug("DevAuth enabled. Injected user {UserId} with roles: {Roles}", userId, roles);

        return next(context);
    }
}
