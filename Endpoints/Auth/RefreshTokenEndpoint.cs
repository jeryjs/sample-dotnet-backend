using FastEndpoints;
using System.Text.Json;

namespace BackendApi.Endpoints.Auth;

public record RefreshTokenRequest(string refresh_token);
public record RefreshTokenResponse(string access_token, string refresh_token, int expires_in, string token_type);

/// <summary>
/// Refresh token endpoint - exchanges a refresh_token for a new access_token.
/// Call this when your access_token is about to expire.
/// </summary>
public class RefreshTokenEndpoint : Endpoint<RefreshTokenRequest, object>
{
    private readonly IConfiguration _config;

    public RefreshTokenEndpoint(IConfiguration config)
    {
        _config = config;
    }

    public override void Configure()
    {
        Post("/auth/refresh");
        AllowAnonymous();
        Options(x => x
            .WithTags("Auth")
            .WithSummary("Refresh access token")
            .WithDescription("Exchanges a refresh_token for a new access_token when the access token expires."));
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(req.refresh_token))
        {
            await SendAsync(new { error = "refresh_token is required" }, StatusCodes.Status400BadRequest, ct);
            return;
        }

        var tenant = _config["AZURE_AD__TENANTID"] ?? _config["AzureAd:TenantId"] ?? string.Empty;
        var clientId = _config["AZURE_AD__CLIENTID"] ?? _config["AzureAd:ClientId"] ?? string.Empty;
        var clientSecret = _config["AZURE_AD__CLIENTSECRET"] ?? _config["AzureAd:ClientSecret"] ?? string.Empty;
        var scope = _config["AZURE_AD__SCOPE"] ?? _config["AzureAd:Scope"] ?? $"{_config["AzureAd:Audience"]}/access_as_user";
        var tokenUrl = $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";

        var form = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = req.refresh_token,
            ["scope"] = scope,
            ["client_secret"] = clientSecret
        };

        try
        {
            using var http = new HttpClient();
            var res = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(form), ct);

            if (!res.IsSuccessStatusCode)
            {
                var errorText = await res.Content.ReadAsStringAsync(ct);
                var result = new { error = "Token refresh failed", details = errorText };
                await SendAsync(result, StatusCodes.Status400BadRequest, ct);
                return;
            }

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var accessToken = root.GetProperty("access_token").GetString() ?? string.Empty;
            var refreshToken = root.TryGetProperty("refresh_token", out var r) ? r.GetString() ?? string.Empty : req.refresh_token;
            var expiresIn = root.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;
            var tokenType = root.GetProperty("token_type").GetString() ?? "Bearer";

            var response = new RefreshTokenResponse(accessToken, refreshToken, expiresIn, tokenType);
            await SendAsync(response, StatusCodes.Status200OK, cancellation: ct);
        }
        catch (Exception ex)
        {
            var result = new { error = "Token refresh error", message = ex.Message };
            await SendAsync(result, StatusCodes.Status500InternalServerError, ct);
        }
    }
}
