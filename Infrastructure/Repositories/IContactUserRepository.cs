using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Repositories;

/// <summary>
/// Repository interface for ContactUser entity operations.
/// </summary>
public interface IContactUserRepository
{
    Task<ContactUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ContactUser?> GetByWavIdAsync(string wavId, CancellationToken cancellationToken = default);
    Task<List<ContactUser>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<ContactUser>> SearchAsync(string? firstName, string? lastName, string? jobTitle, CancellationToken cancellationToken = default);
    Task<List<ContactUser>> GetByEntityAsync(string entityId, CancellationToken cancellationToken = default);
    Task<List<ContactUser>> FindAsync(Func<ContactUser, bool> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<ContactUser> CreateAsync(ContactUser contactUser, CancellationToken cancellationToken = default);
    Task<ContactUser?> UpdateAsync(ContactUser contactUser, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
