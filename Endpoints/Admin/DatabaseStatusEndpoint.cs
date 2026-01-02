using BackendApi.Infrastructure.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Authorization;

namespace BackendApi.Endpoints.Admin;

/// <summary>
/// Endpoint to check database status and collection counts.
/// </summary>
public class DatabaseStatusEndpoint : EndpointWithoutRequest
{
    private readonly MongoDbContext _dbContext;

    public DatabaseStatusEndpoint(MongoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/admin/database-status");
        AllowAnonymous(); // For initial setup - in production, secure this endpoint
        Description(b => b
            .WithTags("Admin")
            .Produces<DatabaseStatusResponse>(200)
            .Produces(500));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var isConnected = await _dbContext.PingAsync(ct);

            if (!isConnected)
            {
                await SendAsync(new DatabaseStatusResponse
                {
                    IsConnected = false,
                    Message = "MongoDB connection failed"
                }, 500, ct);
                return;
            }

            var patientCount = await _dbContext.Patients.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<backend_api.Domain.Models.Patient>.Empty,
                cancellationToken: ct);

            var ancillaryCount = await _dbContext.AncillaryUsers.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<backend_api.Domain.Models.AncillaryUser>.Empty,
                cancellationToken: ct);

            var contactCount = await _dbContext.ContactUsers.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<backend_api.Domain.Models.ContactUser>.Empty,
                cancellationToken: ct);

            var response = new DatabaseStatusResponse
            {
                IsConnected = true,
                DatabaseName = _dbContext.Database.DatabaseNamespace.DatabaseName,
                PatientCount = (int)patientCount,
                AncillaryUserCount = (int)ancillaryCount,
                ContactUserCount = (int)contactCount,
                Message = "MongoDB is connected and operational"
            };

            await SendOkAsync(response, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get database status");
            
            await SendAsync(new DatabaseStatusResponse
            {
                IsConnected = false,
                Message = $"Error: {ex.Message}"
            }, 500, ct);
        }
    }
}

public class DatabaseStatusResponse
{
    public bool IsConnected { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public int PatientCount { get; set; }
    public int AncillaryUserCount { get; set; }
    public int ContactUserCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
