using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Patient entity operations.
/// </summary>
public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Patient?> GetByWavIdAsync(string wavId, CancellationToken cancellationToken = default);
    Task<List<Patient>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Patient>> SearchAsync(string? firstName, string? lastName, string? email, string? phone, CancellationToken cancellationToken = default);
    Task<List<Patient>> GetByAgencyAsync(string agencyName, CancellationToken cancellationToken = default);
    Task<List<Patient>> FindAsync(Func<Patient, bool> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<Patient> CreateAsync(Patient patient, CancellationToken cancellationToken = default);
    Task<Patient?> UpdateAsync(Patient patient, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
