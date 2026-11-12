using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using ExcelDataReader;
using StudentPassportMonitor.Models;

namespace StudentPassportMonitor.Services;

public sealed class ExcelImportService
{
    private static readonly Dictionary<string, string[]> HeaderAliases = new()
    {
        ["FullName"] = ["фио", "ф и о", "фамилия имя отчество", "фамилия и о", "фамилия и инициалы", "студент", "обучающийся"],
        ["LastName"] = ["фамилия", "surname", "last name"],
        ["FirstName"] = ["имя", "first name"],
        ["MiddleName"] = ["отчество", "middle name"],
        ["Faculty"] = ["факультет", "отделение", "специальность", "faculty"],
        ["Group"] = ["группа", "учебная группа", "номер группы", "group"],
        ["PassportNumber"] = ["номер паспорта", "паспорт", "серия и номер паспорта", "passport number"],
        ["IssuedBy"] = ["кем выдан", "issued by"],
        ["IssueDate"] = ["дата выдачи", "дата выдачи паспорта", "issue date"],
        ["BirthDate"] = ["дата рождения", "день рождения", "дата рожд", "д р", "birth date", "birthday"],
        ["Status"] = ["статус", "status"]
    };

    public List<Student> Import(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var data = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
        });

        var students = new List<Student>();
        bool foundHeaders = false;

        foreach (DataTable table in data.Tables)
        {
            if (!TryFindHeader(table, out int headerRow, out var columns))
                continue;

            foundHeaders = true;
            for (int rowIndex = headerRow + 1; rowIndex < table.Rows.Count; rowIndex++)
            {
                DataRow row = table.Rows[rowIndex];
                string Get(string key) => columns.TryGetValue(key, out int index)
                    ? Convert.ToString(row[index], CultureInfo.CurrentCulture)?.Trim() ?? ""
                    : "";

                string lastName = Get("LastName");
                string firstName = Get("FirstName");
                string middleName = Get("MiddleName");

                if (columns.ContainsKey("FullName"))
                {
                    var parts = Regex.Split(Get("FullName").Trim(), @"\s+");
                    if (parts.Length >= 2)
                    {
                        lastName = parts[0];
                        firstName = parts[1];
                        middleName = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : "";
                    }
                }

                if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
                    continue;

                object? Cell(string key) => columns.TryGetValue(key, out int index) ? row[index] : null;

                students.Add(new Student
                {
                    LastName = lastName,
                    FirstName = firstName,
                    MiddleName = middleName,
                    Faculty = Get("Faculty"),
                    Group = Get("Group"),
                    PassportNumber = Get("PassportNumber"),
                    IssuedBy = Get("IssuedBy"),
                    IssueDate = ParseDate(Cell("IssueDate")),
                    BirthDate = ParseDate(Cell("BirthDate")),
                    Status = Get("Status")
                });
            }
        }

        if (students.Count > 0)
            return students;

        throw new InvalidDataException(foundHeaders
            ? "Заголовки найдены, но строки с ФИО студентов отсутствуют или не распознаны."
            : "Не найдены заголовки «ФИО» либо пара «Фамилия» и «Имя». Проверьте структуру листа Excel.");
    }

    private static bool TryFindHeader(
        DataTable table, out int headerRow, out Dictionary<string, int> columns)
    {
        headerRow = -1;
        columns = new Dictionary<string, int>();

        // Some exports use two-level merged headings. Find the row containing
        // the student name, then resolve each field from the nearby header rows.
        for (int rowIndex = 0; rowIndex < Math.Min(table.Rows.Count, 100); rowIndex++)
        {
            var candidate = MatchColumns(table.Rows[rowIndex], table.Columns.Count);
            bool hasName = candidate.ContainsKey("FullName") ||
                (candidate.ContainsKey("LastName") && candidate.ContainsKey("FirstName"));
            if (!hasName)
                continue;

            // Supplement the name header with adjacent subheaders, but never
            // use a data row as a header.
            var previous = rowIndex > 0
                ? MatchColumns(table.Rows[rowIndex - 1], table.Columns.Count)
                : new Dictionary<string, int>();
            var next = rowIndex + 1 < table.Rows.Count
                ? MatchColumns(table.Rows[rowIndex + 1], table.Columns.Count)
                : new Dictionary<string, int>();

            foreach (var (field, index) in previous)
                candidate.TryAdd(field, index);
            foreach (var (field, index) in next)
                candidate.TryAdd(field, index);

            headerRow = rowIndex;
            columns = candidate;
            return true;
        }

        return false;
    }

    private static Dictionary<string, int> MatchColumns(DataRow row, int columnCount)
    {
        var result = new Dictionary<string, int>();

        for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            string cell = Normalize(Convert.ToString(row[columnIndex]));
            if (cell.Length == 0) continue;

            foreach (var (field, aliases) in HeaderAliases)
            {
                if (!aliases.Any(alias => cell == alias ||
                    cell.StartsWith(alias + " ", StringComparison.Ordinal)))
                    continue;

                result.TryAdd(field, columnIndex);
                break;
            }
        }

        return result;
    }

    private static string Normalize(string? value)
    {
        string text = (value ?? "").ToLowerInvariant().Replace('ё', 'е');
        text = Regex.Replace(text, @"[^\p{L}\p{N}]+", " ");
        return Regex.Replace(text.Trim(), @"\s+", " ");
    }

    private static DateTime? ParseDate(object? value)
    {
        if (value is DateTime date)
            return date.Date;

        if (value is double number)
        {
            try { return DateTime.FromOADate(number).Date; }
            catch (ArgumentException) { return null; }
        }

        string text = Convert.ToString(value)?.Trim() ?? "";
        if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("ru-RU"),
                DateTimeStyles.None, out var parsed))
            return parsed.Date;

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out parsed))
            return parsed.Date;

        return null;
    }
}
