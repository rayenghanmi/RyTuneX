using System.Text.Json.Serialization;
using RyTuneX.Views;

namespace RyTuneX.Core.Serialization;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]

[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(GitHubRelease))]

public partial class RyTuneXJsonContext : JsonSerializerContext;
