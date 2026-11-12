using StudentPassportMonitor.Models;

namespace StudentPassportMonitor.Services;

/// <summary>
/// Information-only monitoring of passport replacement milestones.
/// An age milestone is not proof that a passport is legally invalid.
/// </summary>
public enum PassportAlertState
{
    Planned,
    Upcoming,
    ReviewRequired,
    ConfirmationRequired
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
        PassportAlertState.Upcoming => $"Скоро {MilestoneAge} лет",
        PassportAlertState.ConfirmationRequired => "Рубеж наступил — проверьте замену",
        _ => "Требуется проверка паспорта"
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

        DateTime? documentedIssue = LatestKnownIssue(student);
        foreach (int age in MilestoneAges)
        {
            DateTime milestone = birthDate.Date.AddYears(age);
            if (documentedIssue is { } issue && issue.Date >= milestone)
                continue;

            int days = (milestone - today.Date).Days;
            PassportAlertState state = days > warningDays
                ? PassportAlertState.Planned
                : days > 0
                    ? PassportAlertState.Upcoming
                    : days >= -90
                        ? PassportAlertState.ConfirmationRequired
                        : PassportAlertState.ReviewRequired;

            return new PassportAlert(student, age, milestone, days, state);
        }

        return null;
    }

    private static DateTime? LatestKnownIssue(Student student)
    {
        DateTime? issued = student.IssueDate?.Date;
        DateTime? replaced = student.LastPassportReplacementDate?.Date;

        if (issued is null) return replaced;
        if (replaced is null) return issued;
        return issued > replaced ? issued : replaced;
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
