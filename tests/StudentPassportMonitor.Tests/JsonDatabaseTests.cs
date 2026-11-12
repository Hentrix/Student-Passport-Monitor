using StudentPassportMonitor.Models;
using StudentPassportMonitor.Services;

namespace StudentPassportMonitor.Tests;

public sealed class JsonDatabaseTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(),
        "StudentPassportMonitor.Tests", Guid.NewGuid().ToString("N"));

    private JsonDatabaseService CreateDatabase() => new(_directory);

    [Fact]
    public void Сохранение_и_загрузка_студентов_сохраняет_данные()
    {
        var database = CreateDatabase();
        var student = new Student
        {
            LastName = "Тестов", FirstName = "Иван", Group = "ПО-41",
            BirthDate = new DateTime(2006, 3, 14),
            LastPassportReplacementDate = new DateTime(2026, 3, 14)
        };
        database.SaveStudents([student]);

        var loaded = new JsonDatabaseService(_directory).LoadStudents();

        Assert.Single(loaded);
        Assert.Equal(student.Id, loaded[0].Id);
        Assert.Equal(student.BirthDate, loaded[0].BirthDate);
        Assert.Equal(student.LastPassportReplacementDate, loaded[0].LastPassportReplacementDate);
    }

    [Fact]
    public void Восстановление_возвращает_содержимое_копии_и_сохраняет_текущее()
    {
        var database = CreateDatabase();
        database.SaveStudents([new Student { LastName = "До" }]);
        var backup = database.CreateBackup();
        database.SaveStudents([new Student { LastName = "После" }]);

        database.RestoreBackup(backup);

        Assert.Equal("До", Assert.Single(database.LoadStudents()).LastName);
        Assert.True(database.ListBackups().Count >= 2);
    }

    [Fact]
    public void Восстановление_чужого_файла_запрещено()
    {
        var database = CreateDatabase();
        var external = Path.Combine(_directory, "outside.json");
        File.WriteAllText(external, "[]");

        Assert.Throws<ArgumentException>(() => database.RestoreBackup(external));
    }

    [Fact]
    public void Повреждённая_копия_не_изменяет_основную_базу()
    {
        var database = CreateDatabase();
        database.SaveStudents([new Student { LastName = "Сохранён" }]);
        var backup = database.CreateBackup();
        File.WriteAllText(backup, "{not-json}");

        Assert.Throws<InvalidDataException>(() => database.RestoreBackup(backup));
        Assert.Equal("Сохранён", Assert.Single(database.LoadStudents()).LastName);
    }

    [Fact]
    public void Настройки_и_история_проверок_сохраняются()
    {
        var database = CreateDatabase();
        database.SaveSettings(new AppSettings { WarningDays = 30, StartWithWindows = true });
        database.SaveHistory([new CheckHistoryEntry(new DateTime(2026, 11, 12), 2, true)]);

        var loaded = new JsonDatabaseService(_directory);
        Assert.Equal(30, loaded.LoadSettings().WarningDays);
        Assert.True(loaded.LoadSettings().StartWithWindows);
        Assert.Equal(2, Assert.Single(loaded.LoadHistory()).AttentionCount);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
