using System.Text.Json;
using StudentPassportMonitor.Models;

namespace StudentPassportMonitor.Services;

public sealed class JsonDatabaseService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StudentPassportMonitor");

    private string StudentsPath => Path.Combine(_directory, "students.json");
    private string SettingsPath => Path.Combine(_directory, "settings.json");

    public JsonDatabaseService() => Directory.CreateDirectory(_directory);

    public List<Student> LoadStudents() => Read<List<Student>>(StudentsPath) ?? [];
    public AppSettings LoadSettings() => Read<AppSettings>(SettingsPath) ?? new();

    public void SaveStudents(IReadOnlyCollection<Student> students) =>
        Write(StudentsPath, students);

    public void SaveSettings(AppSettings settings) =>
        Write(SettingsPath, settings);

    private static T? Read<T>(string path)
    {
        if (!File.Exists(path))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"Не удалось прочитать данные из {path}. Исправьте или восстановите файл.", ex);
        }
    }

    private void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(_directory);
        string temporaryPath = path + ".tmp";

        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(value, SerializerOptions));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
