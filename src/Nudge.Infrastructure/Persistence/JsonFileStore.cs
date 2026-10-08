using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nudge.Infrastructure.Persistence;

/// <summary>Reads and writes a list of items as a human-readable JSON file.</summary>
internal sealed class JsonFileStore<T>(string path)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    public string FilePath { get; } = path;

    /// <summary>
    /// Loads the stored items. A missing file yields an empty list; an unreadable file is
    /// set aside as a backup instead of being overwritten, so its data can still be recovered.
    /// </summary>
    public List<T> Load()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }
        try
        {
            using var stream = File.OpenRead(FilePath);
            return JsonSerializer.Deserialize<List<T>>(stream, Options) ?? [];
        }
        catch (JsonException)
        {
            File.Move(FilePath, $"{FilePath}.corrupt-{DateTime.Now:yyyyMMddHHmmss}", overwrite: true);
            return [];
        }
    }

    /// <summary>Writes the items atomically, so a crash mid-write never leaves a truncated file.</summary>
    public void Save(IEnumerable<T> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var tempPath = FilePath + ".tmp";
        using (var stream = File.Create(tempPath))
        {
            JsonSerializer.Serialize(stream, items, Options);
        }
        File.Move(tempPath, FilePath, overwrite: true);
    }
}
