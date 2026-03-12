using RyTuneX.Core.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RyTuneX.Core.Helpers;

public static class Json
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        TypeInfoResolver = RyTuneXJsonContext.Default
    };

    public static Task<T?> ToObjectAsync<T>(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult<T?>(default);

        var typeInfo = RyTuneXJsonContext.Default.GetTypeInfo(typeof(T));

        return Task.FromResult((T?)JsonSerializer.Deserialize(value, typeInfo!));
    }

    public static Task<string> StringifyAsync<T>(T value)
    {
        var typeInfo = RyTuneXJsonContext.Default.GetTypeInfo(typeof(T));

        return Task.FromResult(
            JsonSerializer.Serialize(value, typeInfo!)
        );
    }
}