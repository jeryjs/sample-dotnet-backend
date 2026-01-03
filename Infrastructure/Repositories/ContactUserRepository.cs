using backend_api.Domain.Models;
using BackendApi.Infrastructure.Data;
using BackendApi.Infrastructure.Tagging;
using MongoDB.Driver;

namespace BackendApi.Infrastructure.Repositories;

/// <summary>
/// MongoDB-based repository implementation for ContactUser entities with automatic tagging.
/// </summary>
public class ContactUserRepository : IContactUserRepository
{
    private readonly MongoDbContext _dbContext;
    private readonly ILogger<ContactUserRepository> _logger;
    private readonly IMongoCollection<ContactUser> _collection;
    private readonly ITaggingService _taggingService;

    public ContactUserRepository(
        MongoDbContext dbContext,
        ILogger<ContactUserRepository> logger,
        ITaggingService taggingService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _collection = dbContext.ContactUsers;
        _taggingService = taggingService;
    }

    public async Task<ContactUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<ContactUser>.Filter.Eq(c => c.Id, id);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contact user by ID: {Id}", id);
            throw;
        }
    }

    public async Task<ContactUser?> GetByWavIdAsync(string wavId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<ContactUser>.Filter.Eq(c => c.ContactWavId, wavId);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contact user by WAV ID: {WavId}", wavId);
            throw;
        }
    }

    public async Task<List<ContactUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(FilterDefinition<ContactUser>.Empty)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all contact users");
            throw;
        }
    }

    public async Task<List<ContactUser>> SearchAsync(
        string? firstName,
        string? lastName,
        string? jobTitle,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filterBuilder = Builders<ContactUser>.Filter;
            var filters = new List<FilterDefinition<ContactUser>>();

            if (!string.IsNullOrWhiteSpace(firstName))
            {
                filters.Add(filterBuilder.Regex(
                    c => c.FirstName,
                    new MongoDB.Bson.BsonRegularExpression(firstName, "i")));
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                filters.Add(filterBuilder.Regex(
                    c => c.LastName,
                    new MongoDB.Bson.BsonRegularExpression(lastName, "i")));
            }

            if (!string.IsNullOrWhiteSpace(jobTitle))
            {
                filters.Add(filterBuilder.Regex(
                    c => c.JobTitle,
                    new MongoDB.Bson.BsonRegularExpression(jobTitle, "i")));
            }

            var finalFilter = filters.Count > 0
                ? filterBuilder.And(filters)
                : FilterDefinition<ContactUser>.Empty;

            return await _collection.Find(finalFilter).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching contact users");
            throw;
        }
    }

    public async Task<List<ContactUser>> GetByEntityAsync(string entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(entityId, out var entityGuid))
            {
                _logger.LogWarning("Invalid entity ID format: {EntityId}", entityId);
                return new List<ContactUser>();
            }

            var filter = Builders<ContactUser>.Filter.ElemMatch(
                c => c.AssociatedEntities,
                Builders<AssociatedEntity>.Filter.Eq("id", entityGuid));
            
            return await _collection.Find(filter).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contact users by entity: {EntityId}", entityId);
            throw;
        }
    }

    public async Task<List<ContactUser>> FindAsync(Func<ContactUser, bool> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            var allUsers = await GetAllAsync(cancellationToken);
            return allUsers.Where(predicate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding contact users with predicate");
            throw;
        }
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _collection.CountDocumentsAsync(
                FilterDefinition<ContactUser>.Empty,
                cancellationToken: cancellationToken);
            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting contact users");
            throw;
        }
    }

    public async Task<ContactUser> CreateAsync(ContactUser contactUser, CancellationToken cancellationToken = default)
    {
        try
        {
            // Apply tags before insertion
            var taggedContact = await _taggingService.ApplyTagsAsync(
                contactUser,
                operation: "create",
                performedBy: contactUser.ContactOwner,
                dryRun: false,
                cancellationToken: cancellationToken);

            await _collection.InsertOneAsync(taggedContact, cancellationToken: cancellationToken);
            
            _logger.LogInformation(
                "Created contact user with ID: {Id}, applied {TagCount} tags",
                taggedContact.Id,
                taggedContact.Tags.Count);
            
            return taggedContact;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact user");
            throw;
        }
    }

    public async Task<ContactUser?> UpdateAsync(ContactUser contactUser, CancellationToken cancellationToken = default)
    {
        try
        {
            // Apply/update tags before update
            var taggedContact = await _taggingService.ApplyTagsAsync(
                contactUser,
                operation: "update",
                performedBy: null,
                dryRun: false,
                cancellationToken: cancellationToken);

            var filter = Builders<ContactUser>.Filter.Eq(c => c.Id, taggedContact.Id);
            var result = await _collection.ReplaceOneAsync(filter, taggedContact, cancellationToken: cancellationToken);
            
            if (result.MatchedCount == 0)
            {
                _logger.LogWarning("Contact user not found for update: {Id}", taggedContact.Id);
                return null;
            }

            _logger.LogInformation(
                "Updated contact user with ID: {Id}, current tag count: {TagCount}",
                taggedContact.Id,
                taggedContact.Tags.Count);
            
            return taggedContact;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact user");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<ContactUser>.Filter.Eq(c => c.Id, id);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken);
            
            if (result.DeletedCount == 0)
            {
                _logger.LogWarning("Contact user not found for deletion: {Id}", id);
                return false;
            }

            _logger.LogInformation("Deleted contact user with ID: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact user");
            throw;
        }
    }
}
