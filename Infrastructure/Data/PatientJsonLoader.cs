using System.Text.Json;
using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Data;

/// <summary>
/// Specialized JSON data loader for patient records that handles the nested structure
/// with count and patients array.
/// </summary>
public class PatientJsonLoader
{
    private readonly ILogger<PatientJsonLoader> _logger;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Wrapper class for the patient JSON structure.
    /// </summary>
    private class PatientDataWrapper
    {
        public int Count { get; set; }
        public List<Patient> Patients { get; set; } = new();
    }

    public PatientJsonLoader(ILogger<PatientJsonLoader> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    /// <summary>
    /// Loads patient data from the configured JSON file using efficient streaming.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>List of Patient objects or empty list if file not found or on error.</returns>
    public async Task<List<Patient>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var filePath = _configuration["DataFiles:PatientsDataPath"] ?? "all_patients_data_f.json";
        
        try
        {
            if (File.Exists("/app/files/all_patients_data_f.json"))
            {
                filePath = "/app/files/all_patients_data_f.json";
            }
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Patient data file not found: {FilePath}", filePath);
                return new List<Patient>();
            }

            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("Loading patient data from {FilePath} (Size: {FileSize:N0} bytes)", 
                filePath, fileInfo.Length);

            await using var stream = File.OpenRead(filePath);
            var wrapper = await JsonSerializer.DeserializeAsync<PatientDataWrapper>(
                stream, 
                _jsonOptions, 
                cancellationToken);

            if (wrapper?.Patients == null)
            {
                _logger.LogWarning("Patient data wrapper is null or patients array is null");
                return new List<Patient>();
            }

            _logger.LogInformation(
                "Successfully loaded {Count} patients from {FilePath} (Expected: {ExpectedCount})", 
                wrapper.Patients.Count, 
                filePath,
                wrapper.Count);

            if (wrapper.Count != wrapper.Patients.Count)
            {
                _logger.LogWarning(
                    "Patient count mismatch: Expected {Expected}, Got {Actual}", 
                    wrapper.Count, 
                    wrapper.Patients.Count);
            }

            return wrapper.Patients;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error while loading patient data from {FilePath}", filePath);
            return new List<Patient>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading patient data from {FilePath}", filePath);
            return new List<Patient>();
        }
    }
}
