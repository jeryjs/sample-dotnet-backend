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
- ⏳ JSON data loading from files
- ⏳ Patient and User endpoints

### Phase 2 (Future)
- 🔲 Authentication (JWT)
- 🔲 Authorization (Role-based)
- 🔲 Database integration (EF Core)
- 🔲 Caching layer
- 🔲 Rate limiting
- 🔲 API versioning

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
