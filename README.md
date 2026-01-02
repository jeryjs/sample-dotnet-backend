# Backend API with MongoDB

FastEndpoints-based REST API with MongoDB database, Azure AD authentication, and 45+ core endpoints (patients, contacts, ancillaries) plus OAuth2 token exchange and protected write operations.

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- **MongoDB** (via Docker or local installation)
- Azure AD tenant (for OAuth2; optional for local dev with DevAuth)

### Setup

1. **Start MongoDB and Import Data:**
   ```powershell
   cd backend-api
   .\setup-mongodb.ps1
   ```
   
   This automated script will:
   - Start MongoDB using Docker
   - Build the application
   - Import all JSON data into MongoDB
   - Verify everything works

2. **Manual Setup (Alternative):**
   
   **Start MongoDB:**
   ```bash
   docker-compose -f docker-compose.mongodb.yml up -d
   ```
   
   **Configure:**
   - Copy `.env.example` to `.env`
   - Fill in your Azure AD values (TenantId, ClientId, ClientSecret, Scope)
   - Or use DevAuth for local testing: set `DEV_AUTH__ENABLED=true` in `.env`
   
   **Run:**
   ```bash
   dotnet restore
   dotnet run
   ```
   
   **Import Data:**
   ```bash
   curl -X POST http://localhost:5000/api/admin/import-data
   ```

3. **Verify:**
   - API: `http://localhost:5000`
   - Swagger: `http://localhost:5000/swagger`
   - Health: `http://localhost:5000/health`
   - MongoDB UI: `http://localhost:8081` (admin/admin)

## 📊 Database

### MongoDB Collections
- **patients** - Patient records (from `all_patients_data_f.json`)
- **ancillary_users** - Ancillary service providers (from `getActiveAncillaryUsers.json`)
- **contact_users** - Contact information (from `getActiveContactUsers.json`)

### Admin Endpoints (New)
- `POST /api/admin/import-data` — Import JSON files into MongoDB
- `GET /api/admin/database-status` — Check database connection and counts

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
# Azure AD Configuration
AZURE_AD__TENANTID=<tenant-id>
AZURE_AD__CLIENTID=<client-id>
AZURE_AD__CLIENTSECRET=<secret>
AZURE_AD__SCOPE=api://<client-id>/access_as_user

# DevAuth (Local Testing)
DEV_AUTH__ENABLED=true|false
DEV_AUTH__USERID=<user>
DEV_AUTH__USERNAME=<email>
DEV_AUTH__ROLES=Admin,Writer,etc

# MongoDB Configuration
MONGODB__CONNECTIONSTRING=mongodb://localhost:27017
MONGODB__DATABASENAME=WAVBackendAPI
```

## 🐳 Docker

### Start MongoDB (Development):
```bash
docker-compose -f docker-compose.mongodb.yml up -d
```

### Stop MongoDB:
```bash
docker-compose -f docker-compose.mongodb.yml down
```

### Full Application:
```bash
docker-compose up -d
```

Environment variables set in `docker-compose.yml`. Use Docker secrets or Key Vault for production credentials.

## 📚 Documentation

- **[MongoDB-Setup.md](MongoDB-Setup.md)** - Complete MongoDB setup guide
- **[MongoDB-Migration-Complete.md](MongoDB-Migration-Complete.md)** - Migration details
- **[AzureAuth.md](AzureAuth.md)** - Azure AD authentication guide

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
