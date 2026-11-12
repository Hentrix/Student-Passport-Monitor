using StudentPassportMonitor.Models;
using StudentPassportMonitor.Services;

namespace StudentPassportMonitor.Tests;

public sealed class StudentMergeTests
{
    [Fact]
    public void ExistingStudentKeepsIdAndManuallyMaintainedData()
    {
        var id = Guid.NewGuid();
        var reviewed = new DateTime(2026, 1, 2);
        var existing = new Student
        {
            Id = id, LastName = "Тестов", FirstName = "Иван", Group = "А-1",
            PassportNumber = "TEST-ONLY", Faculty = "Факультет",
            LastReviewedAt = reviewed,
            LastPassportReplacementDate = new DateTime(2025, 1, 1)
        };
        var imported = new Student
        {
            LastName = " Тестов ", FirstName = "ИВАН", Group = "а-1",
            PassportNumber = "", Faculty = ""
        };

        var result = new StudentMergeService().Merge([existing], [imported]);
        Assert.Single(result);
        Assert.Equal(id, result[0].Id);
        Assert.Equal("TEST-ONLY", result[0].PassportNumber);
        Assert.Equal("Факультет", result[0].Faculty);
        Assert.Equal(reviewed, result[0].LastReviewedAt);
        Assert.Equal(new DateTime(2025, 1, 1), result[0].LastPassportReplacementDate);
    }

    [Fact]
    public void NewStudentIsAppended()
    {
        var result = new StudentMergeService().Merge(
            [new Student { LastName = "Один", FirstName = "А", Group = "1" }],
            [new Student { LastName = "Другой", FirstName = "Б", Group = "1" }]);
        Assert.Equal(2, result.Count);
    }
}
