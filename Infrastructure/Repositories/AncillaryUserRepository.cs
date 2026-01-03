using backend_api.Domain.Models;
using BackendApi.Infrastructure.Data;
using BackendApi.Infrastructure.Tagging;
using MongoDB.Driver;

namespace BackendApi.Infrastructure.Repositories;

/// <summary>
/// MongoDB-based repository implementation for AncillaryUser entities with automatic tagging.
/// </summary>
public class AncillaryUserRepository : IAncillaryUserRepository
{
    private readonly MongoDbContext _dbContext;
    private readonly ILogger<AncillaryUserRepository> _logger;
    private readonly IMongoCollection<AncillaryUser> _collection;
    private readonly ITaggingService _taggingService;

    public AncillaryUserRepository(
        MongoDbContext dbContext,
        ILogger<AncillaryUserRepository> logger,
        ITaggingService taggingService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _collection = dbContext.AncillaryUsers;
        _taggingService = taggingService;
    }

    public async Task<AncillaryUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<AncillaryUser>.Filter.Eq(a => a.Id, id);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ancillary user by ID: {Id}", id);
            throw;
        }
    }

    public async Task<AncillaryUser?> GetByWavIdAsync(string wavId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<AncillaryUser>.Filter.Eq(a => a.EntityWavId, wavId);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ancillary user by WAV ID: {WavId}", wavId);
            throw;
        }
    }

    public async Task<List<AncillaryUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(FilterDefinition<AncillaryUser>.Empty)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all ancillary users");
            throw;
        }
    }

    public async Task<List<AncillaryUser>> SearchAsync(
        string? name, 
        string? entityType, 
        string? state, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filterBuilder = Builders<AncillaryUser>.Filter;
            var filters = new List<FilterDefinition<AncillaryUser>>();

            if (!string.IsNullOrWhiteSpace(name))
            {
                filters.Add(filterBuilder.Regex(
                    a => a.Name,
                    new MongoDB.Bson.BsonRegularExpression(name, "i")));
            }

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                filters.Add(filterBuilder.Eq(a => a.EntityType, entityType));
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                filters.Add(filterBuilder.Regex(
                    a => a.State,
                    new MongoDB.Bson.BsonRegularExpression(state, "i")));
            }

            var finalFilter = filters.Count > 0
                ? filterBuilder.And(filters)
                : FilterDefinition<AncillaryUser>.Empty;

            return await _collection.Find(finalFilter).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching ancillary users");
            throw;
        }
    }

    public async Task<List<AncillaryUser>> GetByDivisionAsync(string division, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<AncillaryUser>.Filter.Regex(
                "division",
                new MongoDB.Bson.BsonRegularExpression(division, "i"));
            
            return await _collection.Find(filter).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ancillary users by division: {Division}", division);
            throw;
        }
    }

    public async Task<List<AncillaryUser>> FindAsync(Func<AncillaryUser, bool> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            var allUsers = await GetAllAsync(cancellationToken);
            return allUsers.Where(predicate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding ancillary users with predicate");
            throw;
        }
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _collection.CountDocumentsAsync(
                FilterDefinition<AncillaryUser>.Empty,
                cancellationToken: cancellationToken);
            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting ancillary users");
            throw;
        }
    }

    public async Task<AncillaryUser> CreateAsync(AncillaryUser ancillaryUser, CancellationToken cancellationToken = default)
    {
        try
        {
            // Apply tags before insertion
            var taggedAncillary = await _taggingService.ApplyTagsAsync(
                ancillaryUser,
                operation: "create",
                performedBy: null,
                dryRun: false,
                cancellationToken: cancellationToken);

            await _collection.InsertOneAsync(taggedAncillary, cancellationToken: cancellationToken);
            
            _logger.LogInformation(
                "Created ancillary user with ID: {Id}, applied {TagCount} tags",
                taggedAncillary.Id,
                taggedAncillary.Tags.Count);
            
            return taggedAncillary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ancillary user");
            throw;
        }
    }

    public async Task<AncillaryUser?> UpdateAsync(AncillaryUser ancillaryUser, CancellationToken cancellationToken = default)
    {
        try
        {
            // Apply/update tags before update
            var taggedAncillary = await _taggingService.ApplyTagsAsync(
                ancillaryUser,
                operation: "update",
                performedBy: null,
                dryRun: false,
                cancellationToken: cancellationToken);

            var filter = Builders<AncillaryUser>.Filter.Eq(a => a.Id, taggedAncillary.Id);
            var result = await _collection.ReplaceOneAsync(filter, taggedAncillary, cancellationToken: cancellationToken);
            
            if (result.MatchedCount == 0)
            {
                _logger.LogWarning("Ancillary user not found for update: {Id}", taggedAncillary.Id);
                return null;
            }

            _logger.LogInformation(
                "Updated ancillary user with ID: {Id}, current tag count: {TagCount}",
                taggedAncillary.Id,
                taggedAncillary.Tags.Count);
            
            return taggedAncillary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ancillary user");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<AncillaryUser>.Filter.Eq(a => a.Id, id);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken);
            
            if (result.DeletedCount == 0)
            {
                _logger.LogWarning("Ancillary user not found for deletion: {Id}", id);
                return false;
            }

            _logger.LogInformation("Deleted ancillary user with ID: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ancillary user");
            throw;
        }
    }
}
