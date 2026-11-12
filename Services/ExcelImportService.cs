using System.Data;
using System.Globalization;
using ExcelDataReader;
using StudentPassportMonitor.Models;

namespace StudentPassportMonitor.Services;

public sealed class ExcelImportService
{
    private static readonly Dictionary<string, string[]> HeaderAliases = new()
    {
        ["LastName"] = new[] { "фамилия", "last name", "surname" },
        ["FirstName"] = new[] { "имя", "first name", "name" },
        ["MiddleName"] = new[] { "отчество", "middle name" },
        ["Faculty"] = new[] { "факультет", "faculty" },
        ["Group"] = new[] { "группа", "group" },
        ["PassportNumber"] = new[] { "номер паспорта", "паспорт", "passport number" },
        ["IssuedBy"] = new[] { "кем выдан", "issued by" },
        ["IssueDate"] = new[] { "дата выдачи", "issue date" },
        ["BirthDate"] = new[] { "дата рождения", "день рождения", "birth date", "birthday" },
        ["Status"] = new[] { "статус", "status" }
    };

    public List<Student> Import(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var data = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
        });

        foreach (DataTable table in data.Tables)
        {
            int headerRow = -1;
            var columns = new Dictionary<string, int>();
            for (int r = 0; r < Math.Min(table.Rows.Count, 40); r++)
            {
                var found = new Dictionary<string, int>();
                for (int c = 0; c < table.Columns.Count; c++)
                {
                    string value = Normalize(Convert.ToString(table.Rows[r][c]));
                    foreach (var pair in HeaderAliases)
                        if (pair.Value.Contains(value, StringComparer.OrdinalIgnoreCase))
                            found[pair.Key] = c;
                }
                if (found.ContainsKey("LastName") && found.ContainsKey("FirstName"))
                { headerRow = r; columns = found; break; }
            }
            if (headerRow < 0) continue;

            var result = new List<Student>();
            for (int r = headerRow + 1; r < table.Rows.Count; r++)
            {
                var row = table.Rows[r];
                string Get(string key) => columns.TryGetValue(key, out int c)
                    ? Convert.ToString(row[c])?.Trim() ?? "" : "";
                var student = new Student
                {
                    LastName = Get("LastName"), FirstName = Get("FirstName"),
                    MiddleName = Get("MiddleName"), Faculty = Get("Faculty"),
                    Group = Get("Group"), PassportNumber = Get("PassportNumber"),
                    IssuedBy = Get("IssuedBy"), Status = Get("Status"),
                    IssueDate = ParseDate(columns.TryGetValue("IssueDate", out int ic) ? row[ic] : null),
                    BirthDate = ParseDate(columns.TryGetValue("BirthDate", out int bc) ? row[bc] : null)
                };
                if (!string.IsNullOrWhiteSpace(student.LastName) &&
                    !string.IsNullOrWhiteSpace(student.FirstName))
                    result.Add(student);
            }
            if (result.Count > 0) return result;
        }
        throw new InvalidDataException("Не найдена таблица с колонками «Фамилия» и «Имя».");
    }

    private static string Normalize(string? value) =>
        (value ?? "").Trim().ToLowerInvariant().Replace("\n", " ");

    private static DateTime? ParseDate(object? value)
    {
        if (value is DateTime dt) return dt.Date;
        if (value is double number)
        {
            try { return DateTime.FromOADate(number).Date; } catch { return null; }
        }
        string text = Convert.ToString(value)?.Trim() ?? "";
        if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.None, out dt)) return dt.Date;
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) return dt.Date;
        return null;
    }
}