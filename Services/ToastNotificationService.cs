using CommunityToolkit.WinUI.Notifications;

namespace StudentPassportMonitor.Services;

public sealed class ToastNotificationService
{
    public void Show(string title, string message)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }
        catch
        {
            // Toast delivery may be unavailable until the app has a Start Menu shortcut/AUMID.
        }
    }
}