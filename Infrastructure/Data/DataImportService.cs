using System.Text.Json;
using backend_api.Domain.Models;
using MongoDB.Driver;

namespace BackendApi.Infrastructure.Data;

/// <summary>
/// Service for importing JSON data files into MongoDB collections.
/// </summary>
public class DataImportService
{
    private readonly MongoDbContext _dbContext;
    private readonly ILogger<DataImportService> _logger;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    private class PatientDataWrapper
    {
        public int Count { get; set; }
        public List<Patient> Patients { get; set; } = new();
    }

    public DataImportService(
        MongoDbContext dbContext,
        ILogger<DataImportService> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    /// <summary>
    /// Imports all data from JSON files into MongoDB collections.
    /// This is idempotent - it will clear existing data and reimport.
    /// </summary>
    public async Task ImportAllDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting data import process...");

        try
        {
            await ImportPatientsAsync(cancellationToken);
            await ImportAncillaryUsersAsync(cancellationToken);
            await ImportContactUsersAsync(cancellationToken);

            _logger.LogInformation("Data import completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data import failed");
            throw;
        }
    }

    /// <summary>
    /// Imports patient data from JSON file.
    /// </summary>
    public async Task ImportPatientsAsync(CancellationToken cancellationToken = default)
    {
        var filePath = _configuration["DataFiles:PatientsDataPath"] ?? "all_patients_data_f.json";
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Patient data file not found: {FilePath}", filePath);
            return;
        }

        try
        {
            _logger.LogInformation("Importing patients from {FilePath}", filePath);
            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("File size: {Size:N0} bytes", fileInfo.Length);

            await using var stream = File.OpenRead(filePath);
            var wrapper = await JsonSerializer.DeserializeAsync<PatientDataWrapper>(
                stream,
                _jsonOptions,
                cancellationToken);

            if (wrapper?.Patients == null || wrapper.Patients.Count == 0)
            {
                _logger.LogWarning("No patient data found in file");
                return;
            }

            _logger.LogInformation("Parsed {Count} patients from JSON", wrapper.Patients.Count);

            // Clear existing data
            var deleteResult = await _dbContext.Patients.DeleteManyAsync(
                FilterDefinition<Patient>.Empty,
                cancellationToken);
            _logger.LogInformation("Deleted {Count} existing patients", deleteResult.DeletedCount);

            // Insert in batches for better performance
            const int batchSize = 1000;
            var totalInserted = 0;
            var skipped = 0;

            for (int i = 0; i < wrapper.Patients.Count; i += batchSize)
            {
                var batch = wrapper.Patients.Skip(i).Take(batchSize).ToList();
                
                try
                {
                    var options = new InsertManyOptions { IsOrdered = false };
                    await _dbContext.Patients.InsertManyAsync(batch, options, cancellationToken);
                    totalInserted += batch.Count;
                }
                catch (MongoDB.Driver.MongoBulkWriteException<Patient> ex)
                {
                    // Continue on duplicate key errors
                    var dupCount = ex.WriteErrors.Count(e => e.Category == ServerErrorCategory.DuplicateKey);
                    totalInserted += batch.Count - dupCount;
                    skipped += dupCount;
                    _logger.LogWarning("Skipped {Count} duplicate patients in this batch", dupCount);
                }
                
                _logger.LogInformation("Processed batch {Current}/{Total} patients", i + batch.Count, wrapper.Patients.Count);
            }

            _logger.LogInformation("Successfully imported {Count} patients ({Skipped} duplicates skipped)", totalInserted, skipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import patients from {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Imports ancillary user data from JSON file.
    /// </summary>
    public async Task ImportAncillaryUsersAsync(CancellationToken cancellationToken = default)
    {
        var filePath = _configuration["DataFiles:AncillaryUsersDataPath"] ?? "getActiveAncillaryUsers.json";
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Ancillary users data file not found: {FilePath}", filePath);
            return;
        }

        try
        {
            _logger.LogInformation("Importing ancillary users from {FilePath}", filePath);
            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("File size: {Size:N0} bytes", fileInfo.Length);

            await using var stream = File.OpenRead(filePath);
            var ancillaryUsers = await JsonSerializer.DeserializeAsync<List<AncillaryUser>>(
                stream,
                _jsonOptions,
                cancellationToken);

            if (ancillaryUsers == null || ancillaryUsers.Count == 0)
            {
                _logger.LogWarning("No ancillary user data found in file");
                return;
            }

            _logger.LogInformation("Parsed {Count} ancillary users from JSON", ancillaryUsers.Count);

            // Clear existing data
            var deleteResult = await _dbContext.AncillaryUsers.DeleteManyAsync(
                FilterDefinition<AncillaryUser>.Empty,
                cancellationToken);
            _logger.LogInformation("Deleted {Count} existing ancillary users", deleteResult.DeletedCount);

            // Insert in batches
            const int batchSize = 1000;
            var totalInserted = 0;
            var skipped = 0;

            for (int i = 0; i < ancillaryUsers.Count; i += batchSize)
            {
                var batch = ancillaryUsers.Skip(i).Take(batchSize).ToList();
                
                try
                {
                    var options = new InsertManyOptions { IsOrdered = false };
                    await _dbContext.AncillaryUsers.InsertManyAsync(batch, options, cancellationToken);
                    totalInserted += batch.Count;
                }
                catch (MongoDB.Driver.MongoBulkWriteException<AncillaryUser> ex)
                {
                    // Continue on duplicate key errors
                    var dupCount = ex.WriteErrors.Count(e => e.Category == ServerErrorCategory.DuplicateKey);
                    totalInserted += batch.Count - dupCount;
                    skipped += dupCount;
                    _logger.LogWarning("Skipped {Count} duplicate ancillary users in this batch", dupCount);
                }
                
                _logger.LogInformation("Processed batch {Current}/{Total} ancillary users", i + batch.Count, ancillaryUsers.Count);
            }

            _logger.LogInformation("Successfully imported {Count} ancillary users ({Skipped} duplicates skipped)", totalInserted, skipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import ancillary users from {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Imports contact user data from JSON file.
    /// </summary>
    public async Task ImportContactUsersAsync(CancellationToken cancellationToken = default)
    {
        var filePath = _configuration["DataFiles:ContactUsersDataPath"] ?? "getActiveContactUsers.json";
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Contact users data file not found: {FilePath}", filePath);
            return;
        }

        try
        {
            _logger.LogInformation("Importing contact users from {FilePath}", filePath);
            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("File size: {Size:N0} bytes", fileInfo.Length);

            await using var stream = File.OpenRead(filePath);
            var contactUsers = await JsonSerializer.DeserializeAsync<List<ContactUser>>(
                stream,
                _jsonOptions,
                cancellationToken);

            if (contactUsers == null || contactUsers.Count == 0)
            {
                _logger.LogWarning("No contact user data found in file");
                return;
            }

            _logger.LogInformation("Parsed {Count} contact users from JSON", contactUsers.Count);

            // Clear existing data
            var deleteResult = await _dbContext.ContactUsers.DeleteManyAsync(
                FilterDefinition<ContactUser>.Empty,
                cancellationToken);
            _logger.LogInformation("Deleted {Count} existing contact users", deleteResult.DeletedCount);

            // Insert in batches
            const int batchSize = 1000;
            var totalInserted = 0;
            var skipped = 0;

            for (int i = 0; i < contactUsers.Count; i += batchSize)
            {
                var batch = contactUsers.Skip(i).Take(batchSize).ToList();
                
                try
                {
                    var options = new InsertManyOptions { IsOrdered = false };
                    await _dbContext.ContactUsers.InsertManyAsync(batch, options, cancellationToken);
                    totalInserted += batch.Count;
                }
                catch (MongoDB.Driver.MongoBulkWriteException<ContactUser> ex)
                {
                    // Continue on duplicate key errors
                    var dupCount = ex.WriteErrors.Count(e => e.Category == ServerErrorCategory.DuplicateKey);
                    totalInserted += batch.Count - dupCount;
                    skipped += dupCount;
                    _logger.LogWarning("Skipped {Count} duplicate contact users in this batch", dupCount);
                }
                
                _logger.LogInformation("Processed batch {Current}/{Total} contact users", i + batch.Count, contactUsers.Count);
            }

            _logger.LogInformation("Successfully imported {Count} contact users ({Skipped} duplicates skipped)", totalInserted, skipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import contact users from {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Checks if collections have data.
    /// </summary>
    public async Task<bool> IsDataImportedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var patientCount = await _dbContext.Patients.CountDocumentsAsync(
                FilterDefinition<Patient>.Empty,
                cancellationToken: cancellationToken);

            var ancillaryCount = await _dbContext.AncillaryUsers.CountDocumentsAsync(
                FilterDefinition<AncillaryUser>.Empty,
                cancellationToken: cancellationToken);

            var contactCount = await _dbContext.ContactUsers.CountDocumentsAsync(
                FilterDefinition<ContactUser>.Empty,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Current collection counts - Patients: {PatientCount}, Ancillary Users: {AncillaryCount}, Contact Users: {ContactCount}",
                patientCount, ancillaryCount, contactCount);

            return patientCount > 0 || ancillaryCount > 0 || contactCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if data is imported");
            return false;
        }
    }
}
