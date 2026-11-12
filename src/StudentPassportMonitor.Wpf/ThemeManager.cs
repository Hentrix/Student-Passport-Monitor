using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace StudentPassportMonitor.Wpf;

/// <summary>
/// Единая палитра интерфейса. Все цвета берутся из ресурсов приложения.
/// Системный Calendar дополнительно оформляется после построения визуального дерева.
/// </summary>
internal static class ThemeManager
{
    private static Brush Get(string key) =>
        (Brush)Application.Current.Resources[key];

    public static Brush Background => Get("WindowBrush");
    public static Brush Surface => Get("PanelBrush");
    public static Brush Input => Get("InputBrush");
    public static Brush Foreground => Get("TextBrush");
    public static Brush Muted => Get("MutedBrush");
    public static Brush Accent => Get("AccentBrush");
    public static Brush Outline => Get("BorderBrush");

    public static void Initialize()
    {
        EventManager.RegisterClassHandler(typeof(Calendar), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnCalendarLoaded));
    }

    private static void OnCalendarLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Calendar calendar || !ReferenceEquals(sender, e.OriginalSource))
            return;

        calendar.DisplayDateChanged -= OnDisplayDateChanged;
        calendar.DisplayDateChanged += OnDisplayDateChanged;
        calendar.DisplayModeChanged -= OnDisplayModeChanged;
        calendar.DisplayModeChanged += OnDisplayModeChanged;
        ApplyCalendar(calendar);
    }

    private static void OnDisplayDateChanged(object? sender, CalendarDateChangedEventArgs e)
    {
        if (sender is Calendar calendar) ApplyCalendar(calendar);
    }

    private static void OnDisplayModeChanged(object? sender, CalendarModeChangedEventArgs e)
    {
        if (sender is Calendar calendar) ApplyCalendar(calendar);
    }

    private static void ApplyCalendar(Calendar calendar)
    {
        // Кнопки дней создаются заново при переходе между месяцами.
        calendar.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            if (!calendar.IsLoaded) return;
            calendar.Background = Surface;
            calendar.Foreground = Foreground;
            PaintCalendarTree(calendar);
        });
    }

    private static void PaintCalendarTree(DependencyObject node)
    {
        if (node is Border border)
        {
            border.Background = Surface;
            border.BorderBrush = Outline;
        }
        else if (node is CalendarDayButton day)
        {
            day.Background = day.IsSelected ? Accent : Surface;
            day.Foreground = day.IsEnabled ? Foreground : Muted;
        }
        else if (node is CalendarButton month)
        {
            month.Background = Surface;
            month.Foreground = Foreground;
        }
        else if (node is Button button)
        {
            button.Background = Surface;
            button.Foreground = Foreground;
        }
        else if (node is TextBlock text)
        {
            text.Foreground = Foreground;
        }
        else if (node is Panel panel)
        {
            panel.Background = Surface;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            PaintCalendarTree(VisualTreeHelper.GetChild(node, i));
    }
}
