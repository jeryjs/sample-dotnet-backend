# Backend API - ASP.NET Core 8 with FastEndpoints

Production-ready REST API built with ASP.NET Core 8, FastEndpoints, and Serilog structured logging.

## 🏗️ Architecture Overview

### Technology Stack
- **Framework**: ASP.NET Core 8.0
- **API Pattern**: FastEndpoints (REPR pattern)
- **Logging**: Serilog with structured logging
- **Validation**: FluentValidation
- **Documentation**: Swagger/OpenAPI
- **Data Storage**: In-memory repositories (Phase 1)

### Project Structure

```
backend-api/
├── Domain/
│   ├── Common/           # Shared base classes and result types
│   │   ├── BaseEntity.cs
│   │   └── Result.cs
│   └── Models/           # Domain entities
│       ├── Patient.cs
│       └── User.cs
├── Infrastructure/
│   ├── Data/             # Data loading utilities
│   │   ├── IJsonDataLoader.cs
│   │   └── JsonDataLoader.cs
│   └── Repositories/     # Repository implementations
│       ├── IRepository.cs
│       └── InMemoryRepository.cs
├── Endpoints/            # FastEndpoints API endpoints
│   └── HealthCheckEndpoint.cs
├── Middleware/           # Custom middleware
│   └── RequestResponseLoggingMiddleware.cs
├── Program.cs            # Application entry point
├── appsettings.json
└── appsettings.Development.json
```

## 🚀 Getting Started

### Prerequisites

#### Option 1: Local Development
- .NET 8.0 SDK or later
- Visual Studio 2022 / Rider / VS Code

#### Option 2: Docker
- Docker Desktop or Docker Engine
- Docker Compose (included with Docker Desktop)

### Installation

#### Local Development

1. **Restore NuGet packages**:
   ```powershell
   cd z:\Downloads\auth-test\backend-api
   dotnet restore
   ```

2. **Build the project**:
   ```powershell
   dotnet build
   ```

3. **Run the application**:
   ```powershell
   dotnet run
   ```

   The API will be available at:
   - HTTP: `http://localhost:5000`
   - HTTPS: `https://localhost:5001`

#### Docker Deployment

1. **Using Docker Compose (Recommended)**:
   ```powershell
   cd z:\Downloads\auth-test\backend-api
   docker-compose up -d
   ```

   The API will be available at:
   - HTTP: `http://localhost:5000`

2. **Using Docker directly**:
   ```powershell
   # Build the image
   docker build -t backend-api:latest .

   # Run the container
   docker run -d `
     --name backend-api `
     -p 5000:8080 `
     -v ${PWD}/../all_patients_data_f.json:/data/all_patients_data_f.json:ro `
     -v ${PWD}/../getActiveAncillaryUsers.json:/data/getActiveAncillaryUsers.json:ro `
     -v ${PWD}/../getActiveContactUsers.json:/data/getActiveContactUsers.json:ro `
     -e DataFiles__PatientsDataPath=/data/all_patients_data_f.json `
     -e DataFiles__ActiveAncillaryUsersPath=/data/getActiveAncillaryUsers.json `
     -e DataFiles__ActiveContactUsersPath=/data/getActiveContactUsers.json `
     backend-api:latest
   ```

3. **Manage the container**:
   ```powershell
   # View logs
   docker-compose logs -f backend-api

   # Stop the container
   docker-compose down

   # Restart the container
   docker-compose restart

   # Rebuild and restart
   docker-compose up -d --build
   ```

4. **Health check**:
   ```powershell
   curl http://localhost:5000/health
   ```

### Development Mode

Run with hot reload:
```powershell
dotnet watch run
```

## 📚 API Documentation

### Swagger UI
Access interactive API documentation at:
- Development: `http://localhost:5000/swagger`

### Endpoints

#### Health Check
- **GET** `/health` - Built-in health check endpoint
- **GET** `/api/health-check` - Detailed health status with version info

Example response:
```json
{
  "status": "Healthy",
  "timestamp": "2024-12-16T10:30:00Z",
  "version": "1.0.0",
  "environment": "Development"
}
```

## 🔧 Configuration

### appsettings.json

Key configuration sections:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  },
  "DataFiles": {
    "PatientsDataPath": "../all_patients_data_f.json",
    "ActiveAncillaryUsersPath": "../getActiveAncillaryUsers.json",
    "ActiveContactUsersPath": "../getActiveContactUsers.json"
  }
}
```

### Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Set to `Development` or `Production`
- `ASPNETCORE_URLS`: Override default URLs

## 🔐 Security (Phase 1)

**⚠️ IMPORTANT**: Authentication and authorization are **disabled** in Phase 1 for development purposes.

- CORS is set to allow all origins
- All endpoints are accessible without authentication
- This configuration is **NOT suitable for production** deployment

## 📝 Logging

Serilog is configured for structured logging with:

- **Console output** with colored, formatted logs
- **Request/Response logging** middleware
- **Contextual enrichment** (Machine name, Thread ID, Request details)

Log levels by namespace:
- Application: `Information` (Debug in Development)
- Microsoft: `Warning`
- System: `Warning`

## 🧪 Testing

Run tests (when implemented):
```powershell
dotnet test
```

## 📦 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| FastEndpoints | 5.30.0 | REST API framework |
| FastEndpoints.Swagger | 5.30.0 | OpenAPI documentation |
| Serilog | 4.1.0 | Structured logging |
| Serilog.AspNetCore | 8.0.3 | ASP.NET Core integration |
| Serilog.Sinks.Console | 6.0.0 | Console logging |
| FluentValidation | 11.10.0 | Request validation |

## � Docker

The project includes Docker support for easy deployment:

- **Dockerfile**: Multi-stage build with .NET 8 SDK and runtime images
- **docker-compose.yml**: Complete orchestration with volume mounts and health checks
- **.dockerignore**: Optimized build context

### Docker Features

- ✅ Multi-stage build (optimized image size)
- ✅ Non-root user for security
- ✅ Health check monitoring
- ✅ Volume mounts for JSON data files
- ✅ Automatic restart policy
- ✅ Port mapping (5000:8080)
- ✅ Environment variable configuration

### Production Deployment

For production, update the following in `docker-compose.yml`:

1. Set appropriate environment variables
2. Configure persistent volumes
3. Add database connection strings
4. Enable authentication/authorization
5. Configure logging outputs
6. Set resource limits

Example resource limits:
```yaml
services:
  backend-api:
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          cpus: '0.5'
          memory: 256M
```

## �🛠️ Development Workflow

### Adding a New Endpoint

1. Create endpoint class in `/Endpoints`:
   ```csharp
   public class GetPatientsEndpoint : EndpointWithoutRequest<List<Patient>>
   {
       public override void Configure()
       {
           Get("/patients");
           AllowAnonymous();
       }

       public override async Task HandleAsync(CancellationToken ct)
       {
           // Implementation
       }
   }
   ```

2. FastEndpoints will automatically discover and register the endpoint

### Adding a New Repository

1. Create entity in `/Domain/Models`
2. Create repository implementation extending `InMemoryRepository<T>`
3. Register in DI container in `Program.cs`

## 📋 Roadmap

### Phase 1 (Current)
- ✅ Project structure and configuration
- ✅ Serilog structured logging
- ✅ FastEndpoints setup
- ✅ Swagger documentation
- ✅ Health check endpoints
- ✅ CORS configuration (allow all)
- ✅ In-memory repositories
- ✅ JSON data loading from files
- ✅ Patient and User endpoints

### Phase 2 (Current)
- ✅ Authentication (Azure AD / Microsoft Identity)
- ✅ Authorization (Policy-based with roles)
- 🔲 Database integration (EF Core)
- 🔲 Caching layer
- 🔲 Rate limiting
- 🔲 API versioning

---

## 🔐 Phase 2: Authentication & Authorization

The API now implements enterprise-grade authentication and authorization using **Azure AD (Microsoft Entra ID)** with the **Microsoft.Identity.Web** library.

### Overview

All write operations (POST, PUT, PATCH, DELETE) are protected and require authentication. Read operations (GET) remain public for Phase 2 but can be secured in future phases.

**Authentication Flow**:
1. User authenticates via Azure AD OAuth 2.0
2. Azure AD issues a JWT access token with claims (user identity, roles, scopes)
3. API validates the token signature and claims
4. Authorization policies check roles/scopes for access control

**Technology Stack**:
- **Microsoft.Identity.Web**: Azure AD integration for ASP.NET Core
- **JWT Bearer Authentication**: Industry-standard token format
- **Policy-based Authorization**: Flexible, role-based access control

### Authorization Policies

Three authorization policies are configured:

| Policy | Required Role | Description | Applied To |
|--------|--------------|-------------|------------|
| **AdminOnly** | `Admin` | Full access to all operations | Admin-only endpoints (future) |
| **WriteAccess** | `Admin` or `Editor` | Create, update, delete operations | POST, PUT, PATCH, DELETE endpoints |
| **ReadAccess** | `Admin`, `Editor`, or `Viewer` | Read-only operations | GET endpoints (currently public) |

**Role Hierarchy**:
- **Admin**: Full access (create, read, update, delete)
- **Editor**: Write and read access (create, read, update, delete)
- **Viewer**: Read-only access (read)

### Protected Endpoints

**Write Operations (Authentication Required)**:
- `POST /patients`, `PUT /patients/{id}`, `PATCH /patients/{id}`, `DELETE /patients/{id}`
- `POST /contacts`, `PUT /contacts/{id}`, `PATCH /contacts/{id}`, `DELETE /contacts/{id}`
- `POST /ancillaries`, `PUT /ancillaries/{id}`, `PATCH /ancillaries/{id}`, `DELETE /ancillaries/{id}`
- All bulk operations: `POST /bulk/*`

**Public Operations (No Authentication)**:
- `GET /health`, `GET /api/health-check`
- All GET endpoints for patients, contacts, ancillaries
- Search and query endpoints

**Why This Design?**
- **Read-heavy workload**: Most operations are queries; keeping them public improves developer experience
- **Data integrity**: Write operations require authentication to prevent unauthorized data modification
- **Gradual rollout**: Easy to extend authentication to read operations in future phases

---

## 🔧 Azure AD Setup Instructions

Follow these steps to register your application in Azure AD and configure authentication.

### Prerequisites
- Azure subscription with Azure AD tenant
- Global Administrator or Application Administrator role
- Visual Studio Code or Azure Portal access

### Step 1: Register Application in Azure AD

1. **Navigate to Azure Portal**:
   - Go to [https://portal.azure.com](https://portal.azure.com)
   - Search for "Azure Active Directory" or "Microsoft Entra ID"

2. **Register New Application**:
   - Click **App registrations** → **New registration**
   - **Name**: `Backend API` (or your preferred name)
   - **Supported account types**: Choose based on your needs:
     - Single tenant (recommended for internal apps)
     - Multi-tenant (for SaaS applications)
   - **Redirect URI**: Leave blank for now (add later for Swagger)
   - Click **Register**

3. **Note Important Values**:
   - **Application (client) ID**: Copy this value (e.g., `12345678-1234-1234-1234-123456789abc`)
   - **Directory (tenant) ID**: Copy this value (e.g., `87654321-4321-4321-4321-cba987654321`)

### Step 2: Create Client Secret

1. **Generate Secret**:
   - In your app registration, go to **Certificates & secrets**
   - Click **New client secret**
   - **Description**: `Backend API Secret`
   - **Expires**: Choose expiration (6 months, 12 months, or 24 months)
   - Click **Add**

2. **Copy Secret Value**:
   - **IMPORTANT**: Copy the secret **Value** immediately (it won't be shown again)
   - Store it securely using user-secrets (see Local Development section)

### Step 3: Expose API and Define Scopes

1. **Expose API**:
   - Go to **Expose an API** → **Add a scope**
   - **Application ID URI**: Accept default (`api://<client-id>`) or customize
   - Click **Save and continue**

2. **Add Scopes**:
   - **Scope name**: `access_as_user`
   - **Who can consent**: Admins and users
   - **Admin consent display name**: `Access Backend API as user`
   - **Admin consent description**: `Allows the app to access the Backend API on behalf of the signed-in user`
   - **User consent display name**: `Access Backend API`
   - **User consent description**: `Allows the app to access the Backend API on your behalf`
   - **State**: Enabled
   - Click **Add scope**

**Tip:** After adding the scope, set `AZURE_AD__SCOPE` in your `.env` (or user-secrets) to `api://{clientId}/access_as_user`. The Authorize URL will request this explicit scope to avoid `invalid_resource` errors.

### Step 4: Define App Roles

1. **Create App Roles**:
   - Go to **App roles** → **Create app role**
   
   **Admin Role**:
   - **Display name**: `Admin`
   - **Allowed member types**: Users/Groups
   - **Value**: `Admin`
   - **Description**: `Full administrative access to all API operations`
   - **Enabled**: Checked
   - Click **Apply**

   **Editor Role**:
   - **Display name**: `Editor`
   - **Allowed member types**: Users/Groups
   - **Value**: `Editor`
   - **Description**: `Read and write access to API data`
   - **Enabled**: Checked
   - Click **Apply**

   **Viewer Role**:
   - **Display name**: `Viewer`
   - **Allowed member types**: Users/Groups
   - **Value**: `Viewer`
   - **Description**: `Read-only access to API data`
   - **Enabled**: Checked
   - Click **Apply**

### Step 5: Assign Roles to Users

1. **Navigate to Enterprise Applications**:
   - Go to Azure AD → **Enterprise applications**
   - Search for your app name (`Backend API`)
   - Click on the application

2. **Assign Users**:
   - Go to **Users and groups** → **Add user/group**
   - **Users**: Select users or groups
   - **Select a role**: Choose Admin, Editor, or Viewer
   - Click **Assign**

### Step 6: Configure Redirect URIs for Swagger

1. **Add Redirect URIs**:
   - Go back to **App registrations** → Your app → **Authentication**
   - Click **Add a platform** → **Web**
   - Add redirect URIs:
     - `https://localhost:5001/swagger/oauth2-redirect.html` (local dev)
     - `https://your-domain.com/swagger/oauth2-redirect.html` (production)
   - **Implicit grant**: Enable **ID tokens** (for Swagger UI)
   - Click **Configure**

2. **Enable Public Client Flow** (Optional, for Postman):
   - Go to **Authentication** → **Advanced settings**
   - **Allow public client flows**: Yes
   - Click **Save**

### Step 7: Grant Admin Consent

1. **Grant Consent**:
   - Go to **API permissions**
   - You should see `User.Read` (Microsoft Graph) by default
   - Click **Grant admin consent for [Your Tenant]**
   - Click **Yes** to confirm

2. **Verify**:
   - Status should show green checkmarks for all permissions

### Step 8: Update appsettings.json

Update your `appsettings.json` with the values from Azure AD:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR-TENANT-ID-HERE",
    "ClientId": "YOUR-CLIENT-ID-HERE",
    "Audience": "api://YOUR-CLIENT-ID-HERE"
  }
}
```

**Do NOT store `ClientSecret` in appsettings.json**. Use user-secrets or environment variables (see next section).

---

## 🔑 Local Development - Secrets Management

**IMPORTANT**: Never commit secrets to source control. Use one of the following methods to manage sensitive configuration.

### Method 1: User Secrets (Recommended for Local Development)

User secrets are stored outside the project directory and are never checked into source control.

1. **Initialize User Secrets**:
   ```powershell
   cd z:\Downloads\auth-test\backend-api
   dotnet user-secrets init
   ```

   This adds a `UserSecretsId` to your `.csproj` file.

2. **Set Client Secret**:
   ```powershell
   dotnet user-secrets set "AzureAd:ClientSecret" "YOUR-SECRET-VALUE-HERE"
   ```

3. **Set Additional Secrets** (if needed):
   ```powershell
   dotnet user-secrets set "AzureAd:TenantId" "YOUR-TENANT-ID"
   dotnet user-secrets set "AzureAd:ClientId" "YOUR-CLIENT-ID"
   ```

4. **List All Secrets**:
   ```powershell
   dotnet user-secrets list
   ```

5. **Remove a Secret**:
   ```powershell
   dotnet user-secrets remove "AzureAd:ClientSecret"
   ```

6. **Clear All Secrets**:
   ```powershell
   dotnet user-secrets clear
   ```

**Storage Location**:
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

### Method 2: Environment Variables

Set environment variables in your terminal session or system:

**PowerShell**:
```powershell
$env:AzureAd__ClientSecret = "YOUR-SECRET-VALUE-HERE"
$env:AzureAd__TenantId = "YOUR-TENANT-ID"
$env:AzureAd__ClientId = "YOUR-CLIENT-ID"
```

**Command Prompt**:
```cmd
set AzureAd__ClientSecret=YOUR-SECRET-VALUE-HERE
set AzureAd__TenantId=YOUR-TENANT-ID
set AzureAd__ClientId=YOUR-CLIENT-ID
```

**Note**: Double underscores (`__`) represent nested configuration keys.

### Method 3: .env File for Docker

For Docker deployments, create a `.env` file in the same directory as `docker-compose.yml`:

```env
# .env file (DO NOT commit to source control)
AZUREAD_CLIENTSECRET=YOUR-SECRET-VALUE-HERE
AZUREAD_TENANTID=YOUR-TENANT-ID
AZUREAD_CLIENTID=YOUR-CLIENT-ID
```

Add `.env` to your `.gitignore`:
```gitignore
.env
*.env
```

Update `docker-compose.yml` to use environment variables:
```yaml
services:
  backend-api:
    environment:
      - AzureAd__ClientSecret=${AZUREAD_CLIENTSECRET}
      - AzureAd__TenantId=${AZUREAD_TENANTID}
      - AzureAd__ClientId=${AZUREAD_CLIENTID}
    env_file:
      - .env
```

### Method 4: Azure Key Vault (Production)

See the "Production Deployment" section below for Azure Key Vault integration.

---

## 🧪 Testing Authentication

Test authentication with Swagger UI, Postman, or curl.

### Option 1: Swagger UI (Easiest)

1. **Start the Application**:
   ```powershell
   dotnet run
   ```

2. **Open Swagger UI**:
   - Navigate to `https://localhost:5001/swagger`

3. **Authorize**:
   - Click the **Authorize** button (lock icon) at the top right
   - You'll see the OAuth2 configuration with your scopes
   - Click **Authorize** in the dialog
   - You'll be redirected to Microsoft login page

4. **Login**:
   - Enter your Azure AD credentials
   - Grant consent if prompted
   - You'll be redirected back to Swagger UI

5. **Test Protected Endpoint**:
   - Expand a write operation (e.g., `POST /patients`)
   - Click **Try it out**
   - Fill in request body
   - Click **Execute**

6. **Verify**:
   - **200 OK**: Success with valid token and permissions
   - **401 Unauthorized**: Missing or invalid token
   - **403 Forbidden**: Valid token but insufficient permissions (wrong role)

### Option 2: Postman (Recommended for API Testing)

1. **Create New Request**:
   - Open Postman
   - Create a new request: `POST https://localhost:5001/patients`

2. **Configure OAuth 2.0**:
   - Go to **Authorization** tab
   - Type: **OAuth 2.0**
   - Click **Get New Access Token**

3. **Configure Token Request**:
   - **Token Name**: `Backend API Token`
   - **Grant Type**: `Authorization Code`
   - **Callback URL**: `https://oauth.pstmn.io/v1/callback` (Postman's default)
   - **Auth URL**: `https://login.microsoftonline.com/{YOUR-TENANT-ID}/oauth2/v2.0/authorize`
   - **Access Token URL**: `https://login.microsoftonline.com/{YOUR-TENANT-ID}/oauth2/v2.0/token`
   - **Client ID**: `YOUR-CLIENT-ID`
   - **Client Secret**: `YOUR-CLIENT-SECRET`
   - **Scope**: `api://YOUR-CLIENT-ID/access_as_user`
   - **State**: (auto-generated)
   - **Client Authentication**: `Send as Basic Auth header`

4. **Get Token**:
   - Click **Request Token**
   - Login with Azure AD credentials
   - Postman will display the access token

5. **Use Token**:
   - Click **Use Token**
   - The token is automatically added to the request header: `Authorization: Bearer <token>`

6. **Send Request**:
   - Add request body (JSON)
   - Click **Send**

7. **Expected Responses**:
   - **200 OK**: Request successful with valid token and permissions
   - **401 Unauthorized**: Missing or invalid token
   - **403 Forbidden**: Valid token but user lacks required role (Admin/Editor)

### Option 3: curl (Command Line)

1. **Get Access Token** (using client credentials flow for service-to-service):
   ```powershell
   $tokenResponse = Invoke-RestMethod -Method Post `
     -Uri "https://login.microsoftonline.com/{YOUR-TENANT-ID}/oauth2/v2.0/token" `
     -Body @{
       client_id = "YOUR-CLIENT-ID"
       client_secret = "YOUR-CLIENT-SECRET"
       scope = "api://YOUR-CLIENT-ID/.default"
       grant_type = "client_credentials"
     }

   $accessToken = $tokenResponse.access_token
   ```

2. **Call Protected Endpoint**:
   ```powershell
   Invoke-RestMethod -Method Post `
     -Uri "https://localhost:5001/patients" `
     -Headers @{
       Authorization = "Bearer $accessToken"
       "Content-Type" = "application/json"
     } `
     -Body '{"firstName":"John","lastName":"Doe",...}'
   ```

### Verify Token Claims in Logs

The API logs authentication details for debugging. Check the console output:

```
[INF] User authenticated: user@contoso.com
[INF] User roles: Admin, Editor
[INF] Token scopes: access_as_user
[INF] Policy 'WriteAccess' succeeded for user user@contoso.com
```

**Common Issues**:
- **401 Unauthorized**: Token is missing, expired, or has invalid signature
  - Solution: Get a new token
- **403 Forbidden**: Token is valid but user lacks required role
  - Solution: Assign correct role in Azure AD Enterprise Apps
- **Token Validation Failed**: Audience mismatch or wrong tenant
  - Solution: Verify `Audience` in `appsettings.json` matches API's `Application ID URI`

### Expected Response Scenarios

| Scenario | HTTP Status | Response |
|----------|-------------|----------|
| Valid token with Admin/Editor role | `200 OK` | Success response with data |
| Valid token with Viewer role (write endpoint) | `403 Forbidden` | `{"error":"Insufficient permissions"}` |
| No token provided | `401 Unauthorized` | `WWW-Authenticate: Bearer` header |
| Invalid/expired token | `401 Unauthorized` | `{"error":"Invalid token"}` |
| Valid token (read endpoint) | `200 OK` | Success response with data |
| No token (read endpoint) | `200 OK` | Success response (public access) |

---

## 🚀 Production Deployment

Secure your production deployment with these best practices.

### 1. Azure Key Vault (Recommended)

Store secrets in Azure Key Vault instead of environment variables or configuration files.

**Setup**:

1. **Create Key Vault**:
   ```powershell
   az keyvault create `
     --name "backend-api-kv" `
     --resource-group "your-resource-group" `
     --location "eastus"
   ```

2. **Add Secrets**:
   ```powershell
   az keyvault secret set `
     --vault-name "backend-api-kv" `
     --name "AzureAd--ClientSecret" `
     --value "YOUR-SECRET-VALUE"
   ```

3. **Enable Managed Identity** (for App Service/Container Apps):
   ```powershell
   az webapp identity assign `
     --name "backend-api" `
     --resource-group "your-resource-group"
   ```

4. **Grant Key Vault Access**:
   ```powershell
   az keyvault set-policy `
     --name "backend-api-kv" `
     --object-id "<managed-identity-principal-id>" `
     --secret-permissions get list
   ```

5. **Update Program.cs**:
   ```csharp
   builder.Configuration.AddAzureKeyVault(
       new Uri("https://backend-api-kv.vault.azure.net/"),
       new DefaultAzureCredential());
   ```

6. **Install NuGet Package**:
   ```powershell
   dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
   dotnet add package Azure.Identity
   ```

### 2. Managed Identity (Highly Recommended)

Use Managed Identity to authenticate without storing credentials.

**Benefits**:
- No secrets to manage or rotate
- Automatic credential rotation by Azure
- Secure access to Azure resources (Key Vault, Storage, Database)

**Types**:
- **System-assigned**: Tied to a single resource (App Service, VM, Container)
- **User-assigned**: Shared across multiple resources

**Enable for Azure App Service**:
```powershell
az webapp identity assign `
  --name "backend-api" `
  --resource-group "your-resource-group"
```

**Enable for Azure Container Apps**:
```powershell
az containerapp identity assign `
  --name "backend-api" `
  --resource-group "your-resource-group" `
  --system-assigned
```

### 3. CORS Configuration

Update CORS for production to allow only trusted origins.

**Development** (current):
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

**Production** (recommended):
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
                  "https://yourdomain.com",
                  "https://app.yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

app.UseCors("Production");
```

**Environment-specific**:
```csharp
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

Add to `appsettings.Production.json`:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://app.yourdomain.com"
    ]
  }
}
```

### 4. HTTPS Enforcement

Enforce HTTPS in production to protect tokens in transit.

**Enable HTTPS Redirection**:
```csharp
// In Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts(); // HTTP Strict Transport Security
}
```

**Configure HSTS**:
```csharp
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});
```

### 5. Environment-Specific Configuration

Use different `appsettings.{Environment}.json` files:

**appsettings.Production.json**:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR-PROD-TENANT-ID",
    "ClientId": "YOUR-PROD-CLIENT-ID",
    "Audience": "api://YOUR-PROD-CLIENT-ID"
  },
  "AllowedHosts": "yourdomain.com"
}
```

### 6. Security Headers

Add security headers to protect against common attacks:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "no-referrer");
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");
    
    await next();
});
```

### 7. Token Validation Settings

Configure strict token validation in production:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        
        // Production settings
        options.TokenValidationParameters.ValidateLifetime = true;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);
    },
    options =>
    {
        builder.Configuration.Bind("AzureAd", options);
    });
```

### 8. Monitoring and Alerts

Set up monitoring for authentication failures:

- **Application Insights**: Track 401/403 responses
- **Azure AD Sign-in Logs**: Monitor authentication attempts
- **Key Vault Access Logs**: Audit secret access
- **Alerts**: Configure alerts for high rates of auth failures

**Application Insights Integration**:
```powershell
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["ApplicationInsights:ConnectionString"]);
```

---

## 🤝 Contributing

1. Follow the established folder structure
2. Use FastEndpoints pattern for all API endpoints
3. Implement FluentValidation for request validation
4. Write unit tests for business logic
5. Use structured logging with Serilog

## 📄 License

[Your License Here]

## 📞 Support

For issues or questions, please contact the development team.
