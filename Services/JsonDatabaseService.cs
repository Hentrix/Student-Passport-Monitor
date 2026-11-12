using System.Text.Json;
using System.Text.Json.Serialization;
using StudentPassportMonitor.Models;

namespace StudentPassportMonitor.Services;

public sealed class JsonDatabaseService
{
    private readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StudentPassportMonitor");
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private string StudentsPath => Path.Combine(_directory, "students.json");
    private string SettingsPath => Path.Combine(_directory, "settings.json");

    public JsonDatabaseService() => Directory.CreateDirectory(_directory);

    public List<Student> LoadStudents() => Read<List<Student>>(StudentsPath) ?? new();
    public AppSettings LoadSettings() => Read<AppSettings>(SettingsPath) ?? new();

    public void SaveStudents(List<Student> students) => Write(StudentsPath, students);
    public void SaveSettings(AppSettings settings) => Write(SettingsPath, settings);

    private T? Read<T>(string path)
    {
        if (!File.Exists(path)) return default;
        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(path), _options); }
        catch (JsonException) { return default; }
    }

    private void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(_directory);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, _options));
        File.Move(temp, path, true);
    }
}