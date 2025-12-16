using BackendApi.Domain.Common;
using System.Collections.Concurrent;

namespace BackendApi.Infrastructure.Repositories;

public class InMemoryRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ConcurrentDictionary<string, T> _data = new();
    protected readonly ILogger<InMemoryRepository<T>> _logger;

    public InMemoryRepository(ILogger<InMemoryRepository<T>> logger)
    {
        _logger = logger;
    }

    public Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _data.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_data.Values.ToList());
    }

    public Task<List<T>> FindAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default)
    {
        var results = _data.Values.Where(predicate).ToList();
        return Task.FromResult(results);
    }

    public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entity.Id))
        {
            entity.Id = Guid.NewGuid().ToString();
        }

        entity.CreatedAt = DateTime.UtcNow;
        _data.TryAdd(entity.Id, entity);
        _logger.LogInformation("Added entity with ID: {EntityId}", entity.Id);
        return Task.FromResult(entity);
    }

    public Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _data[entity.Id] = entity;
        _logger.LogInformation("Updated entity with ID: {EntityId}", entity.Id);
        return Task.FromResult(entity);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var removed = _data.TryRemove(id, out _);
        if (removed)
        {
            _logger.LogInformation("Deleted entity with ID: {EntityId}", id);
        }
        return Task.FromResult(removed);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_data.Count);
    }

    protected void SeedData(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
        {
            _data.TryAdd(entity.Id, entity);
        }
        _logger.LogInformation("Seeded {Count} entities", _data.Count);
    }
}
