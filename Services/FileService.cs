using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RyTuneX.Core.Contracts.Services;
using RyTuneX.Core.Serialization;

namespace RyTuneX.Core.Services;

public class FileService : IFileService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        TypeInfoResolver = RyTuneXJsonContext.Default
    };

    public T? Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (!File.Exists(path))
            return default;

        var json = File.ReadAllText(path);

        var typeInfo = RyTuneXJsonContext.Default.GetTypeInfo(typeof(T));

        return (T?)JsonSerializer.Deserialize(json, typeInfo!);
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var typeInfo = RyTuneXJsonContext.Default.GetTypeInfo(typeof(T));

        var fileContent = JsonSerializer.Serialize(content, typeInfo!);

        File.WriteAllText(
            Path.Combine(folderPath, fileName),
            fileContent,
            Encoding.UTF8
        );
    }

    public void Delete(string folderPath, string fileName)
    {
        if (!string.IsNullOrEmpty(fileName))
        {
            var path = Path.Combine(folderPath, fileName);
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
