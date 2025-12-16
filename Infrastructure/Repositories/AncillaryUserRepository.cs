using backend_api.Domain.Models;
using BackendApi.Infrastructure.Data;
using System.Collections.Concurrent;

namespace BackendApi.Infrastructure.Repositories;

/// <summary>
/// In-memory repository implementation for AncillaryUser entities.
/// </summary>
public class AncillaryUserRepository : IAncillaryUserRepository
{
    private readonly ConcurrentDictionary<Guid, AncillaryUser> _data = new();
    private readonly ILogger<AncillaryUserRepository> _logger;
    private bool _isInitialized = false;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly AncillaryUserJsonLoader _dataLoader;

    public AncillaryUserRepository(ILogger<AncillaryUserRepository> logger, AncillaryUserJsonLoader dataLoader)
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

            var ancillaryUsers = await _dataLoader.LoadAsync(cancellationToken);
            foreach (var ancillaryUser in ancillaryUsers)
            {
                _data.TryAdd(ancillaryUser.Id, ancillaryUser);
            }
            _isInitialized = true;
            _logger.LogInformation("Initialized AncillaryUserRepository with {Count} ancillary users", _data.Count);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<AncillaryUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        _data.TryGetValue(id, out var ancillaryUser);
        return ancillaryUser;
    }

    public async Task<AncillaryUser?> GetByWavIdAsync(string wavId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return _data.Values.FirstOrDefault(au => 
            au.EntityWavId.Equals(wavId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<AncillaryUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return _data.Values.ToList();
    }

    public async Task<List<AncillaryUser>> SearchAsync(
        string? name, 
        string? entityType, 
        string? state, 
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var results = _data.Values.Where(au =>
        {
            bool matches = true;

            if (!string.IsNullOrWhiteSpace(name))
            {
                matches = matches && au.Name.Contains(name, StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                matches = matches && au.EntityType.Contains(entityType, StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                matches = matches && au.State?.Contains(state, StringComparison.OrdinalIgnoreCase) == true;
            }

            return matches;
        }).ToList();

        return results;
    }

    public async Task<List<AncillaryUser>> GetByDivisionAsync(string division, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        // Note: The current AncillaryUser model doesn't have an explicit division field
        // This method could be extended when division information becomes available
        // For now, returns empty list as a placeholder
        _logger.LogWarning("GetByDivisionAsync called but division field not available in current model");
        return new List<AncillaryUser>();
    }

    public async Task<List<AncillaryUser>> FindAsync(Func<AncillaryUser, bool> predicate, CancellationToken cancellationToken = default)
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
