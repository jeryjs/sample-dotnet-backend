using FastEndpoints;
using FastEndpoints.Swagger;
using Serilog;
using BackendApi.Middleware;
using BackendApi.Infrastructure.Data;
using BackendApi.Infrastructure.Repositories;
using BackendApi.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add FastEndpoints
builder.Services.AddFastEndpoints();

// Add Azure AD Authentication
builder.Services.AddAzureAdAuthentication(builder.Configuration);

// Add Authorization Policies
builder.Services.AddAuthorizationPolicies();

// Add Claims Transformation
builder.Services.AddClaimsTransformation();

// Register JSON Data Loaders as Singletons
builder.Services.AddSingleton<PatientJsonLoader>();
builder.Services.AddSingleton<ContactUserJsonLoader>();
builder.Services.AddSingleton<AncillaryUserJsonLoader>();

// Register Repositories as Singletons
builder.Services.AddSingleton<IPatientRepository, PatientRepository>();
builder.Services.AddSingleton<IContactUserRepository, ContactUserRepository>();
builder.Services.AddSingleton<IAncillaryUserRepository, AncillaryUserRepository>();

// Read Azure AD settings from configuration
var tenantId = builder.Configuration["AzureAd:TenantId"];
var clientId = builder.Configuration["AzureAd:ClientId"];
var audience = builder.Configuration["AzureAd:Audience"];

// Add Swagger
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Backend API - Phase 2 (Azure AD Auth)";
        s.Version = "v1";
        s.Description = "Production-ready ASP.NET Core 8 API with FastEndpoints and Azure AD authentication";
    };
    o.ShortSchemaNames = true;
});

// Add CORS - Allow all for Phase 1
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Health Checks
builder.Services.AddHealthChecks();

// Add exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Register DevAuth middleware service (used if DEV_AUTH__ENABLED=true)
builder.Services.AddSingleton<BackendApi.Infrastructure.Security.DevAuthMiddleware>();

// Middleware pipeline order matters:
// 1. RequestIdMiddleware - First, so all subsequent middleware can use the request ID
app.UseMiddleware<RequestIdMiddleware>();

// 2. GlobalExceptionHandler - Catch all exceptions from subsequent middleware
app.UseExceptionHandler();

// 3. ResponseTimeMiddleware - Measure total request time
app.UseMiddleware<ResponseTimeMiddleware>();

// Use Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
    };
});

// Use custom middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// Use CORS
app.UseCors("AllowAll");

// DevAuth middleware for local dev (injects a synthetic user when DEV_AUTH__ENABLED=true)
var devAuthEnabled = builder.Configuration.GetValue<bool?>("DEV_AUTH__ENABLED") ?? false;
if (devAuthEnabled)
{
    app.UseMiddleware<BackendApi.Infrastructure.Security.DevAuthMiddleware>();
}

// Use Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Use FastEndpoints
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
    c.Serializer.Options.PropertyNamingPolicy = null; // Preserve property names as-is
});

// Use Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
    Log.Information("Swagger UI available at /swagger");
    Log.Information("Azure AD OAuth2 configuration:");
    Log.Information("  Tenant: {Tenant}", tenantId);
    Log.Information("  ClientId: {ClientId}", clientId);
    Log.Information("  Auth URL: https://login.microsoftonline.com/{Tenant}/oauth2/v2.0/authorize", tenantId);
    Log.Information("  Token URL: https://login.microsoftonline.com/{Tenant}/oauth2/v2.0/token", tenantId);
    Log.Information("  For authentication, configure your Azure AD app with redirect URI: http://localhost:5000/signin-oidc");
}

// Map Health Check endpoint with authentication status
app.MapGet("/health", (HttpContext httpContext) =>
{
    var response = new
    {
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        Authentication = new
        {
            IsAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false,
            UserIdentity = httpContext.User.Identity?.Name ?? "Anonymous",
            AuthenticationType = httpContext.User.Identity?.AuthenticationType ?? "None",
            Claims = httpContext.User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        }
    };
    return Results.Ok(response);
}).AllowAnonymous();

app.MapGet("/", () => Results.Redirect("/swagger/v1/swagger.json")).AllowAnonymous();

try
{
    Log.Information("Starting Backend API application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
