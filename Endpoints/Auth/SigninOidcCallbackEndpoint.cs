using FastEndpoints;
using System.Net.Http.Headers;
using System.Text.Json;

namespace BackendApi.Endpoints.Auth;

/// <summary>
/// OAuth2 callback endpoint - receives authorization code from Azure AD and exchanges it for tokens.
/// This is where Azure redirects the user after they sign in.
/// The actual token is NOT stored here; it's returned to the client for them to store and use.
/// </summary>
public class SigninOidcCallbackEndpoint : EndpointWithoutRequest<object>
{
    private readonly IConfiguration _config;

    public SigninOidcCallbackEndpoint(IConfiguration config)
    {
        _config = config;
    }

    public override void Configure()
    {
        Get("/signin-oidc");
        AllowAnonymous();
        Options(x => x
            .WithTags("Auth")
            .WithSummary("OAuth2 callback (signin-oidc)")
            .WithDescription("Callback endpoint for Azure AD authorization. Receives code and exchanges it for tokens."));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var code = HttpContext.Request.Query["code"].ToString();
        var error = HttpContext.Request.Query["error"].ToString();
        var errorDesc = HttpContext.Request.Query["error_description"].ToString();

        // If there's an error from Azure, return it
        if (!string.IsNullOrEmpty(error))
        {
            var errorResponse = new
            {
                error,
                error_description = errorDesc,
                message = $"Azure AD returned an error. {error}: {errorDesc}"
            };
            await SendAsync(errorResponse, StatusCodes.Status400BadRequest, ct);
            return;
        }

        if (string.IsNullOrEmpty(code))
        {
            await SendAsync(new { error = "No authorization code received from Azure AD" }, StatusCodes.Status400BadRequest, ct);
            return;
        }

        // Exchange code for tokens
        var tenant = _config["AZURE_AD__TENANTID"] ?? _config["AzureAd:TenantId"] ?? string.Empty;
        var clientId = _config["AZURE_AD__CLIENTID"] ?? _config["AzureAd:ClientId"] ?? string.Empty;
        var clientSecret = _config["AZURE_AD__CLIENTSECRET"] ?? Environment.GetEnvironmentVariable("AZURE_AD__CLIENTSECRET") ?? _config["AzureAd:ClientSecret"] ?? string.Empty;
        var scope = _config["AZURE_AD__SCOPE"] ?? _config["AzureAd:Scope"] ?? $"{_config["AzureAd:Audience"]}/access_as_user";

        var tokenUrl = $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";
        var redirectUri = $"http{(HttpContext.Request.Host.Value.StartsWith("localhost") ? "" : "s")}://{HttpContext.Request.Host}/api/signin-oidc";

        var form = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["scope"] = scope,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["client_secret"] = clientSecret
        };

        try
        {
            using var http = new HttpClient();
            var res = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(form), ct);

            if (!res.IsSuccessStatusCode)
            {
                var errorText = await res.Content.ReadAsStringAsync(ct);
                var result = new { error = "Token exchange failed", details = errorText };
                await SendAsync(result, StatusCodes.Status400BadRequest, ct);
                return;
            }

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var accessToken = root.GetProperty("access_token").GetString() ?? string.Empty;
            var refreshToken = root.TryGetProperty("refresh_token", out var r) ? r.GetString() ?? string.Empty : string.Empty;
            var expiresIn = root.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;
            var tokenType = root.GetProperty("token_type").GetString() ?? "Bearer";

            // Return tokens to client
            // In production: set these in HttpOnly cookies or return to frontend for storage
            var response = new
            {
                access_token = accessToken,
                refresh_token = refreshToken,
                expires_in = expiresIn,
                token_type = tokenType,
                message = "Token exchange successful. Use the access_token in Authorization: Bearer <token> header for API calls."
            };

            await SendAsync(response, StatusCodes.Status200OK, ct);
        }
        catch (Exception ex)
        {
            var result = new { error = "Token exchange error", message = ex.Message };
            await SendAsync(result, StatusCodes.Status500InternalServerError, ct);
        }
    }
}
