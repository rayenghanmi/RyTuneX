using System.Text.Json;
using System.Text.Json.Serialization;

namespace RyTuneX.Core.Helpers;

public static class Json
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static Task<T?> ToObjectAsync<T>(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult<T?>(default);

        var result = JsonSerializer.Deserialize<T>(value, Options);
        return Task.FromResult(result);
    }

    public static Task<string> StringifyAsync(object value)
    {
        var json = JsonSerializer.Serialize(value, Options);
        return Task.FromResult(json);
    }
}