using backend_api.Domain.Models;
using BackendApi.Infrastructure.Data;
using System.Collections.Concurrent;

namespace BackendApi.Infrastructure.Repositories;

/// <summary>
/// In-memory repository implementation for Patient entities.
/// </summary>
public class PatientRepository : IPatientRepository
{
    private readonly ConcurrentDictionary<string, Patient> _data = new();
    private readonly ILogger<PatientRepository> _logger;
    private bool _isInitialized = false;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly PatientJsonLoader _dataLoader;

    public PatientRepository(ILogger<PatientRepository> logger, PatientJsonLoader dataLoader)
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

            var patients = await _dataLoader.LoadAsync(cancellationToken);
            foreach (var patient in patients)
            {
                _data.TryAdd(patient.Id, patient);
            }
            _isInitialized = true;
            _logger.LogInformation("Initialized PatientRepository with {Count} patients", _data.Count);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<Patient?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        _data.TryGetValue(id, out var patient);
        return patient;
    }

    public async Task<Patient?> GetByWavIdAsync(string wavId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return _data.Values.FirstOrDefault(p => 
            p.AgencyInfo?.PatientWAVId?.Equals(wavId, StringComparison.OrdinalIgnoreCase) == true);
    }

    public async Task<List<Patient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return _data.Values.ToList();
    }

    public async Task<List<Patient>> SearchAsync(
        string? firstName, 
        string? lastName, 
        string? email, 
        string? phone, 
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var results = _data.Values.Where(p =>
        {
            var agencyInfo = p.AgencyInfo;
            if (agencyInfo == null) return false;

            bool matches = true;

            if (!string.IsNullOrWhiteSpace(firstName))
            {
                matches = matches && agencyInfo.PatientFName?.Contains(firstName, StringComparison.OrdinalIgnoreCase) == true;
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                matches = matches && agencyInfo.PatientLName?.Contains(lastName, StringComparison.OrdinalIgnoreCase) == true;
            }

            // Email and phone would need to be added to the model if needed
            // For now, we'll just use the available fields

            return matches;
        }).ToList();

        return results;
    }

    public async Task<List<Patient>> GetByAgencyAsync(string agencyName, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        // Note: The current Patient model doesn't have an explicit agency name field
        // This method could be extended when agency information becomes available
        // For now, returns empty list as a placeholder
        _logger.LogWarning("GetByAgencyAsync called but agency name field not available in current model");
        return new List<Patient>();
    }

    public async Task<List<Patient>> FindAsync(Func<Patient, bool> predicate, CancellationToken cancellationToken = default)
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
