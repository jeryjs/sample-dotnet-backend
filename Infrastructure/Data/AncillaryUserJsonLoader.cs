using System.Text.Json;
using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Data;

/// <summary>
/// Specialized JSON data loader for ancillary user records.
/// </summary>
public class AncillaryUserJsonLoader
{
    private readonly ILogger<AncillaryUserJsonLoader> _logger;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    public AncillaryUserJsonLoader(ILogger<AncillaryUserJsonLoader> logger, IConfiguration configuration)
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
    /// Loads ancillary user data from the configured JSON file.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>List of AncillaryUser objects or empty list if file not found or on error.</returns>
    public async Task<List<AncillaryUser>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var filePath = _configuration["DataFiles:ActiveAncillaryUsersPath"] ?? "getActiveAncillaryUsers.json";
        
        try
        {
            if (!File.Exists(filePath)) filePath = "/data/getActiveAncillaryUsers.json";
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Ancillary users data file not found: {FilePath}", filePath);
                return new List<AncillaryUser>();
            }

            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("Loading ancillary users from {FilePath} (Size: {FileSize:N0} bytes)", 
                filePath, fileInfo.Length);

            await using var stream = File.OpenRead(filePath);
            var ancillaryUsers = await JsonSerializer.DeserializeAsync<List<AncillaryUser>>(
                stream, 
                _jsonOptions, 
                cancellationToken);

            if (ancillaryUsers == null)
            {
                _logger.LogWarning("Ancillary users deserialization returned null");
                return new List<AncillaryUser>();
            }

            _logger.LogInformation(
                "Successfully loaded {Count} ancillary users from {FilePath}", 
                ancillaryUsers.Count, 
                filePath);

            return ancillaryUsers;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error while loading ancillary users from {FilePath}", filePath);
            return new List<AncillaryUser>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading ancillary users from {FilePath}", filePath);
            return new List<AncillaryUser>();
        }
    }
}
