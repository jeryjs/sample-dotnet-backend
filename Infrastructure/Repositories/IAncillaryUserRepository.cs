using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Repositories;

/// <summary>
/// Repository interface for AncillaryUser entity operations.
/// </summary>
public interface IAncillaryUserRepository
{
    Task<AncillaryUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AncillaryUser?> GetByWavIdAsync(string wavId, CancellationToken cancellationToken = default);
    Task<List<AncillaryUser>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<AncillaryUser>> SearchAsync(string? name, string? entityType, string? state, CancellationToken cancellationToken = default);
    Task<List<AncillaryUser>> GetByDivisionAsync(string division, CancellationToken cancellationToken = default);
    Task<List<AncillaryUser>> FindAsync(Func<AncillaryUser, bool> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
