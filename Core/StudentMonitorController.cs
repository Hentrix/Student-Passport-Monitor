using System.Globalization;
using StudentPassportMonitor.Models;
using StudentPassportMonitor.Services;

namespace StudentPassportMonitor.Core;

public sealed record StudentRow(
    Student Student, string FullName, string Group, string Faculty,
    string BirthDate, string PassportNumber, string Age, string DueDate,
    string State, PassportAlertState? AlertStatus);

public enum StudentSortOrder
{
    Name,
    DueDateAscending,
    DueDateDescending
}

public sealed record DashboardSummary(
    int Total, int Upcoming, int ReviewRequired, DateTime? LastChecked);

public sealed class StudentMonitorController
{
    private readonly JsonDatabaseService _storage;
    private readonly ExcelImportService _excel;
    private readonly PassportExpirationService _passport;
    private readonly StudentMergeService _merger;
    private readonly StartupService _startup;
    private List<Student> _students;

    public AppSettings Settings { get; }
    public bool StartupEnabled => _startup.IsEnabled();

    public StudentMonitorController(
        JsonDatabaseService storage,
        ExcelImportService excel,
        PassportExpirationService passport,
        StudentMergeService merger,
        StartupService startup)
    {
        _storage = storage;
        _excel = excel;
        _passport = passport;
        _merger = merger;
        _startup = startup;
        _students = _storage.LoadStudents();
        Settings = _storage.LoadSettings();
    }

    public DashboardSummary GetSummary(DateTime today)
    {
        var alerts = _passport.GetAlerts(_students, today, Settings.WarningDays);
        return new DashboardSummary(
            _students.Count,
            alerts.Count(a => a.Status == PassportAlertState.Upcoming),
            alerts.Count(a => a.Status == PassportAlertState.ReviewRequired),
            Settings.LastDailyCheckDate);
    }

    public IReadOnlyList<StudentRow> GetRows(DateTime today, string search, string group, bool attentionOnly = false, StudentSortOrder sort = StudentSortOrder.Name)
    {
        var matching = _students
            .Where(s => (string.IsNullOrWhiteSpace(search) ||
                         s.FullName.Contains(search, StringComparison.CurrentCultureIgnoreCase)) &&
                        (group == "Все группы" ||
                         s.Group.Equals(group, StringComparison.CurrentCultureIgnoreCase)));

        if (attentionOnly)
            matching = matching.Where(s => _passport.GetAlert(s, today, Settings.WarningDays)
                is { Status: PassportAlertState.Upcoming or PassportAlertState.ReviewRequired });

        IOrderedEnumerable<Student> ordered = sort switch
        {
            StudentSortOrder.DueDateAscending => matching
                .OrderBy(s => _passport.GetAlert(s, today, Settings.WarningDays)?.DueDate ?? DateTime.MaxValue)
                .ThenBy(s => s.LastName).ThenBy(s => s.FirstName),
            StudentSortOrder.DueDateDescending => matching
                .OrderByDescending(s => _passport.GetAlert(s, today, Settings.WarningDays)?.DueDate ?? DateTime.MinValue)
                .ThenBy(s => s.LastName).ThenBy(s => s.FirstName),
            _ => matching.OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
        };

        return ordered
            .Select(s =>
            {
                var alert = _passport.GetAlert(s, today, Settings.WarningDays);
                return new StudentRow(s, s.FullName, s.Group, s.Faculty,
                    s.BirthDate?.ToString("dd.MM.yyyy") ?? "—",
                    s.PassportNumber,
                    s.BirthDate is { } birth
                        ? (today.Year - birth.Year -
                           (today.Date < birth.AddYears(today.Year - birth.Year).Date ? 1 : 0)).ToString()
                        : "—",
                    alert?.DueDate.ToString("dd.MM.yyyy") ?? "—",
                    alert?.State ?? (s.BirthDate is null ? "Нет даты рождения" : "Нет активных рубежей"),
                    alert?.Status);
            })
            .ToArray();
    }

    public IReadOnlyList<string> GetGroups() =>
        _students.Select(s => s.Group)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(s => s)
            .ToArray();

    public int Import(string filePath, bool replace)
    {
        List<Student> imported = _excel.Import(filePath);
        List<Student> updated = replace ? imported : _merger.Merge(_students, imported);
        _storage.SaveStudents(updated);
        _students = updated;
        return imported.Count;
    }

    public void SaveStudent(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        if (string.IsNullOrWhiteSpace(student.LastName) ||
            string.IsNullOrWhiteSpace(student.FirstName))
            throw new ArgumentException("Укажите фамилию и имя.");
        if (student.BirthDate is { } birth &&
            (birth.Date > DateTime.Today || birth.Year < 1900))
            throw new ArgumentException("Проверьте дату рождения.");
        if (student.IssueDate is { } issued && issued.Date > DateTime.Today)
            throw new ArgumentException("Дата выдачи паспорта не может быть в будущем.");
        if (student.LastPassportReplacementDate is { } replaced &&
            (replaced.Date > DateTime.Today ||
             (student.BirthDate is { } born && replaced.Date < born.Date)))
            throw new ArgumentException("Проверьте дату замены паспорта.");

        var updated = _students.ToList();
        int index = updated.FindIndex(s => s.Id == student.Id);
        if (index >= 0) updated[index] = student;
        else updated.Add(student);

        _storage.SaveStudents(updated);
        _students = updated;
    }

    public void DeleteStudent(Guid studentId)
    {
        var updated = _students.Where(s => s.Id != studentId).ToList();
        if (updated.Count == _students.Count)
            throw new InvalidOperationException("Студент не найден.");

        _storage.SaveStudents(updated);
        _students = updated;
    }

    public void SaveReplacement(Guid studentId, string text)
    {
        Student student = _students.FirstOrDefault(s => s.Id == studentId)
            ?? throw new InvalidOperationException("Студент не найден.");

        DateTime? replacement = null;
        if (!string.IsNullOrWhiteSpace(text))
        {
            if (!DateTime.TryParseExact(text.Trim(), "dd.MM.yyyy",
                    CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.None, out var parsed) ||
                parsed.Date > DateTime.Today ||
                (student.BirthDate is { } birth && parsed.Date < birth.Date))
                throw new ArgumentException("Введите дату ДД.ММ.ГГГГ, не позже сегодняшнего дня и не раньше рождения.");

            replacement = parsed.Date;
        }

        var old = student.LastPassportReplacementDate;
        student.LastPassportReplacementDate = replacement;
        try { _storage.SaveStudents(_students); }
        catch { student.LastPassportReplacementDate = old; throw; }
    }

    public void SaveSettings(int warningDays, bool startWithWindows)
    {
        if (warningDays is < 1 or > 730)
            throw new ArgumentOutOfRangeException(nameof(warningDays));

        _startup.SetEnabled(startWithWindows);
        Settings.WarningDays = warningDays;
        Settings.StartWithWindows = startWithWindows;
        _storage.SaveSettings(Settings);
    }

    public IReadOnlyList<PassportAlert> CheckDaily(DateTime today, bool force)
    {
        if (!force && Settings.LastDailyCheckDate?.Date == today.Date)
            return [];

        var alerts = _passport.GetAlerts(_students, today, Settings.WarningDays)
            .Where(a => a.Status != PassportAlertState.Planned)
            .ToArray();

        Settings.LastDailyCheckDate = today.Date;
        _storage.SaveSettings(Settings);
        return alerts;
    }
}
