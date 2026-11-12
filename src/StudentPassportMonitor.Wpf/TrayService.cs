using System.Drawing;
using System.Windows.Forms;

namespace StudentPassportMonitor.Wpf;

/// <summary>Постоянный значок приложения и сгруппированные уведомления Windows.</summary>
internal sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly Icon? _ownedIcon;
    public event Action? OpenRequested;
    public event Action? AttentionRequested;
    public event Action? ExitRequested;

    public TrayService()
    {
        _ownedIcon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? "");
        _icon = new NotifyIcon
        {
            Icon = _ownedIcon ?? SystemIcons.Application,
            Text = "Student Passport Monitor",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };
        _icon.ContextMenuStrip.Items.Add("Открыть", null, (_, _) => OpenRequested?.Invoke());
        _icon.ContextMenuStrip.Items.Add("Требуют внимания", null, (_, _) => AttentionRequested?.Invoke());
        _icon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        _icon.ContextMenuStrip.Items.Add("Выход", null, (_, _) => ExitRequested?.Invoke());
        _icon.DoubleClick += (_, _) => OpenRequested?.Invoke();
        _icon.BalloonTipClicked += (_, _) => AttentionRequested?.Invoke();
    }

    public void Notify(int upcoming, int review)
    {
        int total = upcoming + review;
        if (total == 0) return;
        _icon.BalloonTipTitle = "Контроль паспортов";
        _icon.BalloonTipText = $"Скоро контрольная дата: {upcoming}. Требуют проверки: {review}. Нажмите, чтобы открыть список.";
        _icon.BalloonTipIcon = ToolTipIcon.Info;
        _icon.ShowBalloonTip(5000);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
        _ownedIcon?.Dispose();
    }
}
