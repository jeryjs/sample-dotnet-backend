using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackendApi.Infrastructure.Data;

/// <summary>
/// Custom JSON converter for Guid that handles invalid GUID formats gracefully.
/// Converts invalid GUIDs to empty Guid (00000000-0000-0000-0000-000000000000).
/// </summary>
public class GuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            return reader.GetGuid();
        }
        catch (FormatException)
        {
            // If the value is not a valid GUID, try to read it as a string and parse
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return Guid.Empty;
                }

                // Try to parse the string as a GUID
                if (Guid.TryParse(stringValue, out var guid))
                {
                    return guid;
                }

                // If it's not a valid GUID format, return empty GUID
                return Guid.Empty;
            }

            // For other token types, return empty GUID
            return Guid.Empty;
        }
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
