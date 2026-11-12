using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace StudentPassportMonitor.Wpf;

public partial class App : Application
{
    private static readonly Brush CalendarText = new SolidColorBrush(Color.FromRgb(31, 35, 53));

    public App()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Системная тема WPF переопределяет цвета внутри CalendarItem.
        // Исправляем внутренние элементы после создания их визуального дерева.
        EventManager.RegisterClassHandler(typeof(Calendar), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(CalendarLoaded));
    }

    private static void CalendarLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Calendar calendar) return;

        calendar.DisplayDateChanged -= CalendarDisplayChanged;
        calendar.DisplayDateChanged += CalendarDisplayChanged;
        calendar.DisplayModeChanged -= CalendarModeChanged;
        calendar.DisplayModeChanged += CalendarModeChanged;
        RefreshCalendar(calendar);
    }

    private static void CalendarDisplayChanged(object? sender, CalendarDateChangedEventArgs e)
    {
        if (sender is Calendar calendar) RefreshCalendar(calendar);
    }

    private static void CalendarModeChanged(object? sender, CalendarModeChangedEventArgs e)
    {
        if (sender is Calendar calendar) RefreshCalendar(calendar);
    }

    private static void RefreshCalendar(Calendar calendar)
    {
        calendar.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            if (!calendar.IsLoaded) return;
            ApplyCalendarTextColor(calendar);
        });
    }

    private static void ApplyCalendarTextColor(DependencyObject parent)
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);

            // Состояния выбранного дня и недоступных дат сохраняет WPF.
            // Исправляем только обычный текст, который иначе почти белый.
            if (child is CalendarDayButton day && !day.IsSelected)
                day.Foreground = CalendarText;
            else if (child is CalendarButton monthOrYear)
                monthOrYear.Foreground = CalendarText;
            else if (child is TextBlock text)
                text.Foreground = CalendarText;
            else if (child is Button button && child is not CalendarDayButton)
                button.Foreground = CalendarText;

            ApplyCalendarTextColor(child);
        }
    }
}
