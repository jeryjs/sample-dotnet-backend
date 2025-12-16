using FastEndpoints;
using FastEndpoints.Swagger;
using Serilog;
using BackendApi.Middleware;
using BackendApi.Infrastructure.Data;

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

// Register JSON Data Loaders as Singletons
builder.Services.AddSingleton<PatientJsonLoader>();
builder.Services.AddSingleton<ContactUserJsonLoader>();
builder.Services.AddSingleton<AncillaryUserJsonLoader>();

// Register Repositories as Singletons
builder.Services.AddSingleton<BackendApi.Infrastructure.Repositories.IPatientRepository, BackendApi.Infrastructure.Repositories.PatientRepository>();

// Add Swagger
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Backend API";
        s.Version = "v1";
        s.Description = "Production-ready ASP.NET Core 8 API with FastEndpoints";
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

// No authentication/authorization for Phase 1
// app.UseAuthentication();
// app.UseAuthorization();

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
}

// Map Health Check endpoint
app.MapHealthChecks("/health");

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
