using StudentPassportMonitor.Models;

namespace StudentPassportMonitor.Services;

public sealed class StudentMergeService
{
    // Один студент определяется по ФИО и группе, независимо от регистра и лишних пробелов.
    public static string GetKey(Student student) =>
        string.Join("|", Normalize(student.LastName), Normalize(student.FirstName),
            Normalize(student.MiddleName), Normalize(student.Group));

    public static List<Student> RemoveDuplicates(IEnumerable<Student> students)
    {
        ArgumentNullException.ThrowIfNull(students);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Сохраняем первую запись: повторные строки не должны затирать её данные.
        return students.Where(student => keys.Add(GetKey(student))).ToList();
    }

    public List<Student> Merge(IEnumerable<Student> existing, IEnumerable<Student> imported)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(imported);

        var result = existing.ToList();
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < result.Count; i++)
            index.TryAdd(GetKey(result[i]), i);

        foreach (var student in imported)
        {
            string key = GetKey(student);
            if (!index.TryGetValue(key, out int position))
            {
                index[key] = result.Count;
                result.Add(student);
                continue;
            }

            Student previous = result[position];
            student.Id = previous.Id;
            // Импорт из Excel не должен стирать сведения, внесённые вручную.
            student.LastPassportReplacementDate = previous.LastPassportReplacementDate;
            student.LastReviewedAt = previous.LastReviewedAt;
            student.BirthDate ??= previous.BirthDate;
            student.IssueDate ??= previous.IssueDate;
            if (string.IsNullOrWhiteSpace(student.PassportNumber))
                student.PassportNumber = previous.PassportNumber;
            if (string.IsNullOrWhiteSpace(student.IssuedBy))
                student.IssuedBy = previous.IssuedBy;
            if (string.IsNullOrWhiteSpace(student.Status))
                student.Status = previous.Status;
            if (string.IsNullOrWhiteSpace(student.Faculty))
                student.Faculty = previous.Faculty;
            result[position] = student;
        }

        return result;
    }

    private static string Normalize(string? value) =>
        string.Join(" ", (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
}
