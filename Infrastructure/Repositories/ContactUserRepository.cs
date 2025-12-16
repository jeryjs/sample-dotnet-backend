using backend_api.Domain.Models;
using BackendApi.Infrastructure.Data;
using System.Collections.Concurrent;

namespace BackendApi.Infrastructure.Repositories;

/// <summary>
/// In-memory repository implementation for ContactUser entities.
/// </summary>
public class ContactUserRepository : IContactUserRepository
{
    private readonly ConcurrentDictionary<Guid, ContactUser> _data = new();
    private readonly ILogger<ContactUserRepository> _logger;
    private bool _isInitialized = false;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly ContactUserJsonLoader _dataLoader;

    public ContactUserRepository(ILogger<ContactUserRepository> logger, ContactUserJsonLoader dataLoader)
    {
        _logger = logger;
        _dataLoader = dataLoader;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized) return;

            var contactUsers = await _dataLoader.LoadAsync(cancellationToken);
            foreach (var contactUser in contactUsers)
            {
                _data.TryAdd(contactUser.Id, contactUser);
            }
            _isInitialized = true;
            _logger.LogInformation("Initialized ContactUserRepository with {Count} contact users", _data.Count);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<ContactUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        _data.TryGetValue(id, out var contactUser);
        return contactUser;
    }

    public async Task<ContactUser?> GetByWavIdAsync(string wavId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return _data.Values.FirstOrDefault(cu => 
            cu.ContactWavId.Equals(wavId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<ContactUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return _data.Values.ToList();
    }

    public async Task<List<ContactUser>> SearchAsync(
        string? firstName, 
        string? lastName, 
        string? jobTitle, 
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var results = _data.Values.Where(cu =>
        {
            bool matches = true;

            if (!string.IsNullOrWhiteSpace(firstName))
            {
                matches = matches && cu.FirstName.Contains(firstName, StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                matches = matches && cu.LastName.Contains(lastName, StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(jobTitle))
            {
                matches = matches && cu.JobTitle?.Contains(jobTitle, StringComparison.OrdinalIgnoreCase) == true;
            }

            return matches;
        }).ToList();

        return results;
    }

    public async Task<List<ContactUser>> GetByEntityAsync(string entityId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        var results = _data.Values.Where(cu => 
            cu.AssociatedEntities.Any(ae => 
                ae.Id.ToString().Equals(entityId, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return results;
    }

    public async Task<List<ContactUser>> FindAsync(Func<ContactUser, bool> predicate, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return _data.Values.Where(predicate).ToList();
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return _data.Count;
    }
}
