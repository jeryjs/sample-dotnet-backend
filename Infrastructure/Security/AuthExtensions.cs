using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

namespace BackendApi.Infrastructure.Security;

public static class AuthExtensions
{
    /// <summary>
    /// Configures Azure AD authentication with JWT bearer token validation
    /// </summary>
    public static IServiceCollection AddAzureAdAuthentication(
        this IServiceCollection services,
        IConfiguration config)
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger("AuthExtensions");

        logger.LogInformation("Configuring Azure AD authentication...");

        // Add Microsoft Identity Web API authentication
        services.AddMicrosoftIdentityWebApiAuthentication(config, "AzureAd");

        // Configure JWT Bearer options for additional validation and logging
        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            // Read audience from config
            var audience = config["AzureAd:Audience"] ?? config["AZURE_AD__AUDIENCE"] ?? string.Empty;
            var clientId = config["AzureAd:ClientId"] ?? config["AZURE_AD__CLIENTID"] ?? string.Empty;

            // Token validation parameters
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudiences = new[] { audience, clientId }.Where(a => !string.IsNullOrEmpty(a)).Distinct().ToArray(),
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minute clock skew
            };

            // Log token validation events
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    logger.LogError(
                        "Authentication failed: {Exception}",
                        context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var claims = context.Principal?.Claims
                        .Select(c => $"{c.Type}: {c.Value}")
                        .ToList();

                    logger.LogInformation("Token validated successfully for user: {User}. Claims: {Claims}",
                        context.Principal?.Identity?.Name ?? "Unknown",
                        string.Join(", ", claims ?? new List<string>()));

                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    logger.LogWarning(
                        "Authentication challenge. Error: {Error}, ErrorDescription: {ErrorDescription}",
                        context.Error,
                        context.ErrorDescription);
                    return Task.CompletedTask;
                },
                OnForbidden = context =>
                {
                    logger.LogWarning(
                        "Authorization forbidden for user: {User}",
                        context.Principal?.Identity?.Name ?? "Unknown");
                    return Task.CompletedTask;
                }
            };
        });

        logger.LogInformation("Azure AD authentication configured successfully");

        return services;
    }

    /// <summary>
    /// Configures authorization policies for API access control
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger("AuthExtensions");

        logger.LogInformation("Configuring authorization policies...");

        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin");
                logger.LogInformation("Policy 'AdminOnly' defined: Requires role 'Admin'");
            })
            .AddPolicy("WriteAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    // Check for api.write scope
                    var hasWriteScope = context.User.HasClaim(c =>
                        c.Type == "http://schemas.microsoft.com/identity/claims/scope" &&
                        c.Value.Split(' ').Contains("api.write"));

                    // Check for Admin or Writer role
                    var hasAdminRole = context.User.IsInRole("Admin");
                    var hasWriterRole = context.User.IsInRole("Writer");

                    return hasWriteScope || hasAdminRole || hasWriterRole;
                });
                logger.LogInformation("Policy 'WriteAccess' defined: Requires scope 'api.write' OR role 'Admin' OR role 'Writer'");
            })
            .AddPolicy("ReadAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    // Check for api.read scope
                    var hasReadScope = context.User.HasClaim(c =>
                        c.Type == "http://schemas.microsoft.com/identity/claims/scope" &&
                        c.Value.Split(' ').Contains("api.read"));

                    return hasReadScope;
                });
                logger.LogInformation("Policy 'ReadAccess' defined: Requires scope 'api.read' OR any authenticated user");
            })
            .AddPolicy("DefaultAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                logger.LogInformation("Policy 'DefaultAccess' defined: Requires authenticated user");
            });

        logger.LogInformation("Authorization policies configured successfully");

        return services;
    }

    /// <summary>
    /// Registers claims transformation to map Azure AD groups to roles
    /// </summary>
    public static IServiceCollection AddClaimsTransformation(this IServiceCollection services)
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger("AuthExtensions");

        logger.LogInformation("Registering claims transformation...");

        services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation,
            AzureAdClaimsTransformation>();

        logger.LogInformation(
            "Claims transformation registered: AzureAdClaimsTransformation will map Azure AD groups to roles");

        return services;
    }
}
