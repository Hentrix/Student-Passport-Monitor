namespace StudentPassportMonitor.Models;

public sealed class Student
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string MiddleName { get; set; } = "";
    public string Faculty { get; set; } = "";
    public string Group { get; set; } = "";
    public string PassportNumber { get; set; } = "";
    public string IssuedBy { get; set; } = "";
    public DateTime? IssueDate { get; set; }
    public DateTime? BirthDate { get; set; }
    public string Status { get; set; } = "";
    public DateTime? LastPassportReplacementDate { get; set; }

    public string FullName => string.Join(" ", new[] { LastName, FirstName, MiddleName }
        .Where(x => !string.IsNullOrWhiteSpace(x)));
}