# Sample Backend API

FastEndpoints-based REST API with Azure AD authentication for getting into dotnet. 45+ core endpoints (patients, contacts, ancillaries) plus OAuth2 token exchange and protected write operations.

## Quick Start

### Prerequisites
- .NET 8 SDK
- Azure AD tenant (for OAuth2; optional for local dev with DevAuth)

### Setup

1. **Clone & restore:**
   ```bash
   cd backend-api
   dotnet restore
   ```

2. **Configure:**
   - Copy `.env.example` to `.env`
   - Fill in your Azure AD values (TenantId, ClientId, ClientSecret, Scope)
   - Or use DevAuth for local testing: set `DEV_AUTH__ENABLED=true` in `.env`

3. **Run:**
   ```bash
   dotnet run
   ```

   API available at `http://localhost:5000`  
   Swagger at `http://localhost:5000/swagger`

## Key Endpoints

### Auth Flow
- `GET /api/auth/authorize` — returns Azure login URL
- `GET /api/signin-oidc?code=...` — OAuth2 callback, exchanges code for tokens
- `POST /api/auth/refresh` — refresh access token

### Protected Operations (require auth)
- `POST /api/patients`, `PUT /api/patients/{id}`, etc. — WriteAccess policy
- `GET` endpoints remain public

### Data
- `GET /api/patients` — all patients
- `GET /api/contacts`, `GET /api/ancillaries` — respective entities
- `POST /api/bulk/patients`, etc. — batch ops
- `GET /api/stats/*`, `GET /api/reports/*` — aggregations & reports

## DevAuth (Local Testing)

Set in `.env`:
```
DEV_AUTH__ENABLED=true
DEV_AUTH__USERID=testuser
DEV_AUTH__USERNAME=test@example.com
DEV_AUTH__ROLES=Admin,Writer
```

No Azure AD registration needed. All authenticated endpoints will work with the injected user.

## Azure AD Setup (Production)

1. Register API app in Azure AD
2. Expose API → add scope `access_as_user`
3. Create client secret, set in `.env` as `AZURE_AD__CLIENTSECRET`
4. Register client app (Swagger/frontend), add API permissions & grant consent
5. Set redirect URI: `http://localhost:5000/api/signin-oidc`

See `Azure.md` for detailed flow.

## Environment Variables

Read from `.env` (auto-loaded at startup):
```
AZURE_AD__TENANTID=<tenant-id>
AZURE_AD__CLIENTID=<client-id>
AZURE_AD__CLIENTSECRET=<secret>
AZURE_AD__SCOPE=api://<client-id>/access_as_user
DEV_AUTH__ENABLED=true|false
DEV_AUTH__USERID=<user>
DEV_AUTH__USERNAME=<email>
DEV_AUTH__ROLES=Admin,Writer,etc
```

## Docker

```bash
docker-compose up -d
```

Environment variables set in `docker-compose.yml`. Use Docker secrets or Key Vault for production credentials.

## Testing

### Swagger UI
1. Run API
2. Open `http://localhost:5000/swagger`
3. Call endpoints; protected ops will fail (expected)
4. Use `/api/auth/authorize` to get tokens for auth testing

### Postman
Import `backend-api.postman_collection.json`. Collection pre-configured with OAuth2 (manual token setup).

## Structure

```
Endpoints/
  ├── Auth/
  │   ├── AuthorizeEndpoint.cs
  │   ├── SigninOidcCallbackEndpoint.cs
  │   └── RefreshTokenEndpoint.cs
  ├── Patients/
  ├── Contacts/
  └── Ancillaries/
Infrastructure/
  ├── Security/
  │   ├── AuthExtensions.cs
  │   ├── DevAuthMiddleware.cs
  │   └── AzureAdClaimsTransformation.cs
  ├── Repositories/
  ├── Data/
  └── Middleware/
```

## Notes

- Read endpoints are public by default; write ops require `WriteAccess` policy
- In-memory data storage; no DB layer
- `.env` not committed (in `.gitignore`)
- Logs to console (Serilog)
- Health check at `/health` (includes auth status)
