using StudentPassportMonitor.Models;

namespace StudentPassportMonitor.Services;

public enum PassportAlertState
{
    Planned,
    Upcoming,
    ReviewRequired
}

public sealed record PassportAlert(
    Student Student,
    int MilestoneAge,
    DateTime DueDate,
    int DaysRemaining,
    PassportAlertState Status)
{
    public string State => Status switch
    {
        PassportAlertState.Planned => "Запланировано",
        PassportAlertState.Upcoming => "Скоро",
        _ => "Нужна проверка документа"
    };
}

public sealed class PassportExpirationService
{
    private static readonly int[] MilestoneAges = [20, 45];

    public PassportAlert? GetAlert(Student student, DateTime today, int warningDays)
    {
        ArgumentNullException.ThrowIfNull(student);
        if (student.BirthDate is not { } birthDate)
            return null;

        foreach (int age in MilestoneAges)
        {
            DateTime milestone = birthDate.Date.AddYears(age);
            if (student.LastPassportReplacementDate is { } replacement &&
                replacement.Date >= milestone)
                continue;

            int daysRemaining = (milestone - today.Date).Days;
            PassportAlertState status = daysRemaining < 0
                ? PassportAlertState.ReviewRequired
                : daysRemaining <= warningDays
                    ? PassportAlertState.Upcoming
                    : PassportAlertState.Planned;

            return new PassportAlert(student, age, milestone, daysRemaining, status);
        }

        return null;
    }

    public IReadOnlyList<PassportAlert> GetAlerts(
        IEnumerable<Student> students, DateTime today, int warningDays)
    {
        ArgumentNullException.ThrowIfNull(students);

        return students
            .Select(student => GetAlert(student, today, warningDays))
            .OfType<PassportAlert>()
            .OrderBy(alert => alert.DueDate)
            .ToArray();
    }
}
