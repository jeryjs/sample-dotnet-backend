using backend_api.Domain.Models;
using BackendApi.Infrastructure.Data;
using MongoDB.Driver;

namespace BackendApi.Infrastructure.Repositories;

/// <summary>
/// MongoDB-based repository implementation for Patient entities.
/// </summary>
public class PatientRepository : IPatientRepository
{
    private readonly MongoDbContext _dbContext;
    private readonly ILogger<PatientRepository> _logger;
    private readonly IMongoCollection<Patient> _collection;

    public PatientRepository(
        MongoDbContext dbContext,
        ILogger<PatientRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _collection = dbContext.Patients;
    }

    public async Task<Patient?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<Patient>.Filter.Eq(p => p.Id, id);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patient by ID: {Id}", id);
            throw;
        }
    }

    public async Task<Patient?> GetByWavIdAsync(string wavId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<Patient>.Filter.Eq("agencyInfo.patientWAVId", wavId);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patient by WAV ID: {WavId}", wavId);
            throw;
        }
    }

    public async Task<List<Patient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(FilterDefinition<Patient>.Empty)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all patients");
            throw;
        }
    }

    public async Task<List<Patient>> SearchAsync(
        string? firstName,
        string? lastName,
        string? email,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filterBuilder = Builders<Patient>.Filter;
            var filters = new List<FilterDefinition<Patient>>();

            if (!string.IsNullOrWhiteSpace(firstName))
            {
                filters.Add(filterBuilder.Regex(
                    "agencyInfo.patientFName",
                    new MongoDB.Bson.BsonRegularExpression(firstName, "i")));
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                filters.Add(filterBuilder.Regex(
                    "agencyInfo.patientLName",
                    new MongoDB.Bson.BsonRegularExpression(lastName, "i")));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                filters.Add(filterBuilder.Regex(
                    "agencyInfo.email",
                    new MongoDB.Bson.BsonRegularExpression(email, "i")));
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                filters.Add(filterBuilder.Regex(
                    "agencyInfo.phoneNo",
                    new MongoDB.Bson.BsonRegularExpression(phone, "i")));
            }

            var finalFilter = filters.Count > 0
                ? filterBuilder.And(filters)
                : FilterDefinition<Patient>.Empty;

            return await _collection.Find(finalFilter).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching patients");
            throw;
        }
    }

    public async Task<List<Patient>> GetByAgencyAsync(string agencyName, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<Patient>.Filter.Regex(
                "agencyInfo.agencyName",
                new MongoDB.Bson.BsonRegularExpression(agencyName, "i"));
            
            return await _collection.Find(filter).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patients by agency: {AgencyName}", agencyName);
            throw;
        }
    }

    public async Task<List<Patient>> FindAsync(Func<Patient, bool> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            var allPatients = await GetAllAsync(cancellationToken);
            return allPatients.Where(predicate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding patients with predicate");
            throw;
        }
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _collection.CountDocumentsAsync(
                FilterDefinition<Patient>.Empty,
                cancellationToken: cancellationToken);
            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting patients");
            throw;
        }
    }

    public async Task<Patient> CreateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        try
        {
            await _collection.InsertOneAsync(patient, cancellationToken: cancellationToken);
            _logger.LogInformation("Created patient with ID: {Id}", patient.Id);
            return patient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating patient");
            throw;
        }
    }

    public async Task<Patient?> UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<Patient>.Filter.Eq(p => p.Id, patient.Id);
            var result = await _collection.ReplaceOneAsync(filter, patient, cancellationToken: cancellationToken);
            
            if (result.MatchedCount == 0)
            {
                _logger.LogWarning("Patient not found for update: {Id}", patient.Id);
                return null;
            }

            _logger.LogInformation("Updated patient with ID: {Id}", patient.Id);
            return patient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<Patient>.Filter.Eq(p => p.Id, id);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken);
            
            if (result.DeletedCount == 0)
            {
                _logger.LogWarning("Patient not found for deletion: {Id}", id);
                return false;
            }

            _logger.LogInformation("Deleted patient with ID: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting patient");
            throw;
        }
    }
}
