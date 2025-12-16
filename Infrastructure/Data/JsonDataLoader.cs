using System.Text.Json;

namespace BackendApi.Infrastructure.Data;

public class JsonDataLoader<T> : IJsonDataLoader<T> where T : class
{
    private readonly ILogger<JsonDataLoader<T>> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonDataLoader(ILogger<JsonDataLoader<T>> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    public async Task<T?> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            await using var stream = File.OpenRead(filePath);
            var data = await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken);
            
            _logger.LogInformation("Successfully loaded data from {FilePath}", filePath);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading JSON data from {FilePath}", filePath);
            throw;
        }
    }

    public async Task<List<T>> LoadListAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return new List<T>();
            }

            await using var stream = File.OpenRead(filePath);
            var data = await JsonSerializer.DeserializeAsync<List<T>>(stream, _jsonOptions, cancellationToken);
            
            _logger.LogInformation("Successfully loaded list data from {FilePath}", filePath);
            return data ?? new List<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading JSON list data from {FilePath}", filePath);
            throw;
        }
    }
}
