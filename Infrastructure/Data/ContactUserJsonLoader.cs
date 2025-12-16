using System.Text.Json;
using backend_api.Domain.Models;

namespace BackendApi.Infrastructure.Data;

/// <summary>
/// Specialized JSON data loader for contact user records.
/// </summary>
public class ContactUserJsonLoader
{
    private readonly ILogger<ContactUserJsonLoader> _logger;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    public ContactUserJsonLoader(ILogger<ContactUserJsonLoader> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        _jsonOptions.Converters.Add(new GuidJsonConverter(_logger));
    }
    
    /// <summary>
    /// Custom converter to handle Guid values that may be in non-standard formats.
    /// </summary>
    public class GuidJsonConverter : System.Text.Json.Serialization.JsonConverter<Guid>
    {
        private readonly ILogger _logger;

        public GuidJsonConverter(ILogger logger)
        {
            _logger = logger;
        }

        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (reader.TokenType == JsonTokenType.String)
            {
                if (Guid.TryParse(str, out var guid))
                    return guid;
            }
            // throw new JsonException("Invalid GUID format.");
            _logger?.LogWarning("Failed to parse GUID value: {Value}", str);
            return Guid.Empty;
        }
    
        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// Loads contact user data from the configured JSON file.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>List of ContactUser objects or empty list if file not found or on error.</returns>
    public async Task<List<ContactUser>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var filePath = _configuration["DataFiles:ActiveContactUsersPath"] ?? "getActiveContactUsers.json";
        
        try
        {
            if (File.Exists("/app/files/getActiveContactUsers.json"))
            {
                filePath = "/app/files/getActiveContactUsers.json";
            }
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Contact users data file not found: {FilePath}", filePath);
                return new List<ContactUser>();
            }

            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("Loading contact users from {FilePath} (Size: {FileSize:N0} bytes)", 
                filePath, fileInfo.Length);

            await using var stream = File.OpenRead(filePath);
            var contactUsers = await JsonSerializer.DeserializeAsync<List<ContactUser>>(
                stream, 
                _jsonOptions, 
                cancellationToken);

            if (contactUsers == null)
            {
                _logger.LogWarning("Contact users deserialization returned null");
                return new List<ContactUser>();
            }

            _logger.LogInformation(
                "Successfully loaded {Count} contact users from {FilePath}", 
                contactUsers.Count, 
                filePath);

            return contactUsers;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error while loading contact users from {FilePath}", filePath);
            return new List<ContactUser>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading contact users from {FilePath}", filePath);
            return new List<ContactUser>();
        }
    }
}
