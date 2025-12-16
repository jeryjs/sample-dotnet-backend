# Azure AD OAuth2 Integration

Standard OIDC Authorization Code flow with token exchange and refresh.

## Flow

1. **Authorize** — Frontend/User calls `/api/auth/authorize`, to get Azure login URL and signs in
2. **Callback** — Azure redirects to `/api/signin-oidc?code=...` with auth code upon successful sign in
- 2.5. **Exchange** (when access_token expires later) — `/api/auth/exchange` exchanges code for new `access_token` + `refresh_token` (implicit in callback)
3. **Use** — frontend includes `Authorization: Bearer <access_token>` in API calls
4. **Refresh** — when token expires, `POST /api/auth/refresh` with `refresh_token` → new `access_token`

## Setup

### Azure Portal

**API App Registration:**
- [Create app](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/CreateApplicationBlade/quickStartType~/null/isMSAApp~/false), note the Client ID (use as `AZURE_AD__CLIENTID`)
- [Expose an API](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/appId/) → set Application ID URI: `api://[client-id]`
- Add scope: name = `access_as_user`
- Certificates & secrets → create secret, copy **Value** → `AZURE_AD__CLIENTSECRET`

**Client App Registration** (frontend):
- Create app, note the Client ID
- Authentication → Add platform Web → redirect URIs: `http://localhost:5000/api/signin-oidc`
- API permissions → Add permission → My APIs → [API App] → `access_as_user`
- Grant admin consent

### Config

```
AZURE_AD__TENANTID=<directory-id>
AZURE_AD__CLIENTID=<api-app-client-id>
AZURE_AD__CLIENTSECRET=<secret-value>
AZURE_AD__SCOPE=api://<client-id>/access_as_user
```

## Endpoints

### `/api/auth/authorize` (GET)
Returns Azure login URL. Frontend opens it or redirects user.

```
http://localhost:5000/api/auth/authorize
→ https://login.microsoftonline.com/...?client_id=...&scope=...&redirect_uri=...
```

### `/api/signin-oidc` (GET callback)
Azure redirects here after login with `?code=...`. Backend exchanges code for tokens, returns JSON.

```json
{
  "access_token": "eyJ0eXAi...",
  "refresh_token": "0.AX0...",
  "expires_in": 3600,
  "token_type": "Bearer"
}
```

Frontend stores tokens (localStorage/sessionStorage) and includes `access_token` in subsequent requests.

### `/api/auth/refresh` (POST)
Refresh expired token.

```json
{
  "refresh_token": "0.AX0..."
}
```

Response:
```json
{
  "access_token": "eyJ0eXAi...",
  "refresh_token": "0.AX0...",
  "expires_in": 3600,
  "token_type": "Bearer"
}
```

## Token Validation

Backend validates:
- **Issuer** — must be `https://login.microsoftonline.com/{tenant}/v2.0`
- **Audience** — must be API Application ID URI or ClientID
- **Signature** — verified against Azure AD public keys (OIDC metadata)
- **Lifetime** — valid from `iat` to `exp` (+ 5 min clock skew)

## Authorization Policies

Three policies defined:
- `AdminOnly` — requires `Admin` role
- `WriteAccess` — requires `api.write` scope OR `Admin`/`Writer` role
- `ReadAccess` — requires `api.read` scope OR authenticated

Write endpoints (`POST`, `PUT`, `PATCH`, `DELETE`) require `WriteAccess`.

Usage in endpoint:
```csharp
public override void Configure()
{
    Post("/patients");
    Policies("WriteAccess");
}
```

## Claims & Groups

Azure AD groups are mapped to role claims via `IClaimsTransformation`. If user is member of a group, that group's ID becomes a `role` claim. Policies can check roles directly.

To assign roles: Azure Portal → Enterprise Applications → [API App] → Users and groups → Assign role.

## Local Dev (DevAuth)

No Azure setup needed. Set in `.env`:
```
DEV_AUTH__ENABLED=true
DEV_AUTH__USERID=testuser
DEV_AUTH__ROLES=Admin,Writer
```

Middleware injects a synthetic authenticated principal. Bypasses Azure AD entirely for testing.

## Production Notes

- Store `AZURE_AD__CLIENTSECRET` in Key Vault, not `.env`
- Use Managed Identity for Key Vault access
- Enable HTTPS, set `ValidateIssuer = true` always
- Use `AllowAnonymous()` sparingly; default to `Policies("ReadAccess")` for public GET endpoints if desired
- Log token validation events (already done in `AuthExtensions`)
