namespace StudentPassportMonitor.Services;

public sealed class ToastNotificationService
{
    // Windows tray balloon notification. Native toast integration is planned separately.
    public void Show(string title, string message)
    {
        using var icon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "Student Passport Monitor",
            Visible = true
        };
        icon.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
        // Keep the icon alive briefly: disposing immediately can hide the notification.
        // The application's persistent tray icon is preferred; see MainForm.
    }
}