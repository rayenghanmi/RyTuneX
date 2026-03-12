using System.Text.Json.Serialization;

namespace RyTuneX.Core.Serialization;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]

[JsonSerializable(typeof(List<string>))]

public partial class RyTuneXJsonContext : JsonSerializerContext;
