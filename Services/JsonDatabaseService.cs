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

    private string BackupsPath => Path.Combine(_directory, "backups");
    private string StudentsPath => Path.Combine(_directory, "students.json");
    private string SettingsPath => Path.Combine(_directory, "settings.json");

    public JsonDatabaseService() => Directory.CreateDirectory(_directory);

    public List<Student> LoadStudents() => Read<List<Student>>(StudentsPath) ?? [];
    public AppSettings LoadSettings() => Read<AppSettings>(SettingsPath) ?? new();

    public void SaveStudents(IReadOnlyCollection<Student> students) =>
        Write(StudentsPath, students);

    public string CreateBackup()
    {
        Directory.CreateDirectory(BackupsPath);
        string destination = Path.Combine(BackupsPath,
            $"students-{DateTime.UtcNow:yyyyMMdd-HHmmss-fffffff}.json");
        if (File.Exists(StudentsPath))
            File.Copy(StudentsPath, destination);
        else
            File.WriteAllText(destination, JsonSerializer.Serialize(new List<Student>(), SerializerOptions));
        return destination;
    }

    public void RestoreBackup(string backupPath)
    {
        string fullPath = Path.GetFullPath(backupPath);
        string backupDirectory = Path.GetFullPath(BackupsPath);
        if (!Path.GetDirectoryName(fullPath)!.Equals(backupDirectory,
                StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(fullPath))
            throw new ArgumentException("Выберите существующую резервную копию приложения.");

        List<Student> restored = Read<List<Student>>(fullPath)
            ?? throw new InvalidDataException("Резервная копия пуста или повреждена.");
        CreateBackup();
        SaveStudents(restored);
    }

    public IReadOnlyList<string> ListBackups() =>
        Directory.Exists(BackupsPath)
            ? Directory.GetFiles(BackupsPath, "students-*.json")
                .OrderByDescending(x => x).ToArray()
            : [];


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
        string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

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
