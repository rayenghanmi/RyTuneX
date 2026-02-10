using RyTuneX.Core.Contracts.Services;
using RyTuneX.Core.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        try
        {
            var json = File.ReadAllText(path);
            var typeInfo = RyTuneXJsonContext.Default.GetTypeInfo(typeof(T));
            return (T?)JsonSerializer.Deserialize(json, typeInfo!);
        }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error reading settings from {fileName}: {ex.Message}");
            return default;
        }
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        try
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
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error saving settings to {fileName}: {ex.Message}");
        }
    }

    public void Delete(string folderPath, string fileName)
    {
        if (!string.IsNullOrEmpty(fileName))
        {
            var path = Path.Combine(folderPath, fileName);
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                _ = LogHelper.LogError($"Error deleting file {fileName}: {ex.Message}");
            }
        }
    }
}
