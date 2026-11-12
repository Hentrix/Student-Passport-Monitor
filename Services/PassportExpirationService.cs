using StudentPassportMonitor.Models;

namespace StudentPassportMonitor.Services;

public sealed record PassportAlert(Student Student, int MilestoneAge, DateTime DueDate, int DaysRemaining, string State);

public sealed class PassportExpirationService
{
    public PassportAlert? GetAlert(Student student, DateTime today, int warningDays)
    {
        if (student.BirthDate is not DateTime birth) return null;

        foreach (int age in new[] { 20, 45 })
        {
            DateTime due = birth.Date.AddYears(age);
            if (student.LastPassportReplacementDate is DateTime replaced && replaced.Date >= due)
                continue;

            int days = (due - today.Date).Days;
            string state = days < 0 ? "Нужна проверка документа"
                : days <= warningDays ? "Скоро"
                : "Запланировано";
            return new PassportAlert(student, age, due, days, state);
        }

        return new PassportAlert(student, 45, birth.Date.AddYears(45),
            (birth.Date.AddYears(45) - today.Date).Days, "Нужна проверка документа");
    }

    public IReadOnlyList<PassportAlert> GetAlerts(IEnumerable<Student> students, DateTime today, int warningDays) =>
        students.Select(s => GetAlert(s, today, warningDays))
            .Where(a => a is not null)
            .Select(a => a!)
            .OrderBy(a => a.DueDate)
            .ToList();
}