using StudentPassportMonitor.Models;
using StudentPassportMonitor.Services;

namespace StudentPassportMonitor.Tests;

public sealed class PassportExpirationTests
{
    private readonly PassportExpirationService _service = new();

    [Fact]
    public void NoBirthDateProducesNoAlert()
    {
        Assert.Null(_service.GetAlert(new Student(), new DateTime(2026, 1, 1), 90));
    }

    [Fact]
    public void UpcomingMilestoneIsDetected()
    {
        var student = new Student { BirthDate = new DateTime(2006, 2, 1) };
        var alert = _service.GetAlert(student, new DateTime(2026, 1, 15), 30);
        Assert.NotNull(alert);
        Assert.Equal(20, alert.MilestoneAge);
        Assert.Equal(PassportAlertState.Upcoming, alert.Status);
    }

    [Fact]
    public void MilestoneOnBirthdayRequiresConfirmation()
    {
        var student = new Student { BirthDate = new DateTime(2006, 2, 1) };
        Assert.Equal(PassportAlertState.ConfirmationRequired,
            _service.GetAlert(student, new DateTime(2026, 2, 1), 90)!.Status);
    }

    [Fact]
    public void AfterNinetyDaysRequiresReview()
    {
        var student = new Student { BirthDate = new DateTime(2006, 2, 1) };
        Assert.Equal(PassportAlertState.ReviewRequired,
            _service.GetAlert(student, new DateTime(2026, 5, 3), 90)!.Status);
    }

    [Fact]
    public void IssueAfterTwentiethBirthdayMovesToFortyFive()
    {
        var student = new Student
        {
            BirthDate = new DateTime(2000, 1, 1),
            IssueDate = new DateTime(2020, 2, 1)
        };
        Assert.Equal(45, _service.GetAlert(student, new DateTime(2026, 1, 1), 90)!.MilestoneAge);
    }

    [Fact]
    public void ReplacementAfterFortyFiveClearsAgeMilestones()
    {
        var student = new Student
        {
            BirthDate = new DateTime(1970, 1, 1),
            LastPassportReplacementDate = new DateTime(2015, 2, 1)
        };
        Assert.Null(_service.GetAlert(student, new DateTime(2026, 1, 1), 90));
    }

    [Fact]
    public void AlertsAreSortedByMilestone()
    {
        var first = new Student { BirthDate = new DateTime(2006, 6, 1) };
        var second = new Student { BirthDate = new DateTime(2006, 3, 1) };
        var alerts = _service.GetAlerts([first, second], new DateTime(2026, 1, 1), 90);
        Assert.Same(second, alerts[0].Student);
        Assert.Same(first, alerts[1].Student);
    }
}
