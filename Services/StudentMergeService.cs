using StudentPassportMonitor.Models;

namespace StudentPassportMonitor.Services;

public sealed class StudentMergeService
{
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
            student.LastPassportReplacementDate = previous.LastPassportReplacementDate;
            result[position] = student;
        }

        return result;
    }

    private static string GetKey(Student student) =>
        string.Join("|",
            Normalize(student.LastName),
            Normalize(student.FirstName),
            Normalize(student.MiddleName),
            Normalize(student.Group));

    private static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();
}
