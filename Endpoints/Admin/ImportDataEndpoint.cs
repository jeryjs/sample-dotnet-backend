using BackendApi.Infrastructure.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Authorization;

namespace BackendApi.Endpoints.Admin;

/// <summary>
/// Endpoint to import data from JSON files into MongoDB.
/// </summary>
public class ImportDataEndpoint : EndpointWithoutRequest
{
    private readonly DataImportService _importService;
    private readonly MongoDbContext _dbContext;

    public ImportDataEndpoint(DataImportService importService, MongoDbContext dbContext)
    {
        _importService = importService;
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Post("/admin/import-data");
        AllowAnonymous(); // For initial setup - in production, secure this endpoint
        Description(b => b
            .WithTags("Admin")
            .Produces<ImportDataResponse>(200)
            .Produces(500));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Logger.LogInformation("Starting data import...");

        try
        {
            // Check if data already exists
            var isImported = await _importService.IsDataImportedAsync(ct);
            
            if (isImported)
            {
                Logger.LogWarning("Data already exists in MongoDB. Import will replace existing data.");
            }

            // Create indexes first
            Logger.LogInformation("Creating indexes...");
            await _dbContext.CreateIndexesAsync(ct);

            // Import all data
            await _importService.ImportAllDataAsync(ct);

            // Get final counts
            var patientCount = await _dbContext.Patients.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<backend_api.Domain.Models.Patient>.Empty,
                cancellationToken: ct);

            var ancillaryCount = await _dbContext.AncillaryUsers.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<backend_api.Domain.Models.AncillaryUser>.Empty,
                cancellationToken: ct);

            var contactCount = await _dbContext.ContactUsers.CountDocumentsAsync(
                MongoDB.Driver.FilterDefinition<backend_api.Domain.Models.ContactUser>.Empty,
                cancellationToken: ct);

            var response = new ImportDataResponse
            {
                Success = true,
                Message = "Data import completed successfully",
                PatientsImported = (int)patientCount,
                AncillaryUsersImported = (int)ancillaryCount,
                ContactUsersImported = (int)contactCount
            };

            Logger.LogInformation(
                "Import completed: {PatientCount} patients, {AncillaryCount} ancillary users, {ContactCount} contact users",
                patientCount, ancillaryCount, contactCount);

            await SendOkAsync(response, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Data import failed");
            
            var response = new ImportDataResponse
            {
                Success = false,
                Message = $"Data import failed: {ex.Message}"
            };

            await SendAsync(response, 500, ct);
        }
    }
}

public class ImportDataResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PatientsImported { get; set; }
    public int AncillaryUsersImported { get; set; }
    public int ContactUsersImported { get; set; }
}
