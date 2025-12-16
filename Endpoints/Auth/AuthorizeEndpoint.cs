using FastEndpoints;
using Microsoft.AspNetCore.WebUtilities;

namespace BackendApi.Endpoints.Auth;

/// <summary>
/// Returns a redirect URL for Azure AD authorization endpoint (Authorization Code + PKCE)
/// Useful to open in a browser to begin interactive login and obtain an authorization code.
/// This endpoint is enabled for convenience and should be used alongside the exchange endpoint.
/// </summary>
public class AuthorizeEndpoint : EndpointWithoutRequest<string>
{
    private readonly IConfiguration _config;

    public AuthorizeEndpoint(IConfiguration config)
    {
        _config = config;
    }

    public override void Configure()
    {
        Get("/auth/authorize");
        AllowAnonymous();
        Options(x => x.WithTags("Auth").WithSummary("Get Azure AD authorize URL").WithDescription("Returns the authorization URL to start the OAuth2 Authorization Code flow (PKCE)."));
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        var tenant = _config["AZURE_AD__TENANTID"] ?? _config["AzureAd:TenantId"] ?? string.Empty;
        var clientId = _config["AZURE_AD__CLIENTID"] ?? _config["AzureAd:ClientId"] ?? string.Empty;
        // Prefer an explicit named scope (recommended): AZURE_AD__SCOPE or AzureAd:Scope
        var apiScope = _config["AZURE_AD__SCOPE"] ?? _config["AzureAd:Scope"];
        if (string.IsNullOrEmpty(apiScope))
        {
            var audience = _config["AZURE_AD__AUDIENCE"] ?? _config["AzureAd:Audience"] ?? string.Empty;
            apiScope = !string.IsNullOrEmpty(audience) ? $"{audience}/access_as_user" : string.Empty;
        }

        var redirect = HttpContext.Request.Query.TryGetValue("redirect_uri", out var r) ? r.ToString() : $"http://{HttpContext.Request.Host}/api/signin-oidc";

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["response_type"] = "code",
            ["redirect_uri"] = redirect,
            ["response_mode"] = "query",
            // Use explicit scope name rather than '/.default' which can trigger invalid_resource errors
            ["scope"] = string.IsNullOrEmpty(apiScope) ? "openid profile offline_access" : $"{apiScope} openid profile offline_access",
            ["prompt"] = "select_account"
        };

        var authorizeUrl = QueryHelpers.AddQueryString($"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize", query!);

        return SendAsync(authorizeUrl, cancellation: ct);
    }
}
