namespace StudentPassportMonitor.Models;

public sealed class AppSettings
{
    public int WarningDays { get; set; } = 90;
    public bool StartWithWindows { get; set; }
    public DateTime? LastDailyCheckDate { get; set; }
}