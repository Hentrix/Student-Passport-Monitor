using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace StudentPassportMonitor.Wpf;

/// <summary>Выбор даты без системного Calendar и CalendarItem.</summary>
public partial class DarkDatePicker : UserControl
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    private DateTime _visibleMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private bool _updatingSelectors;
    private bool _updatingText;

    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?),
            typeof(DarkDatePicker), new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public DarkDatePicker()
    {
        InitializeComponent();
        MonthSelector.ItemsSource = Russian.DateTimeFormat.MonthNames.Take(12).ToArray();
        YearSelector.ItemsSource = Enumerable.Range(1900, 201).ToArray();
        DateText.Text = "";
        CalendarPopup.CustomPopupPlacementCallback = PlaceBelowField;
        UpdateSelectors();
        RenderDays();
    }

    private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (DarkDatePicker)d;
        picker._updatingText = true;
        picker.DateText.Text = picker.SelectedDate?.ToString("dd.MM.yyyy", Russian) ?? "";
        picker._updatingText = false;
        if (picker.SelectedDate is { } date)
        {
            picker._visibleMonth = new DateTime(date.Year, date.Month, 1);
            picker.UpdateSelectors();
        }
        picker.RenderDays();
    }

    public bool CommitText()
    {
        if (_updatingText) return true;
        string value = DateText.Text.Trim();
        if (value.Length == 0)
        {
            SelectedDate = null;
            return true;
        }

        if (!DateTime.TryParseExact(value, "dd.MM.yyyy", Russian,
                DateTimeStyles.None, out DateTime date))
            return false;

        SelectedDate = date.Date;
        return true;
    }

    private void DateText_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        // Не стираем некорректный ввод: форма проверит его перед сохранением.
        if (CommitText()) DateText.BorderBrush = (Brush)FindResource("BorderBrush");
        else DateText.BorderBrush = Brushes.IndianRed;
    }

    private void OpenButton_Checked(object sender, RoutedEventArgs e)
    {
        if (CalendarPopup is not null) CalendarPopup.IsOpen = true;
    }

    private static CustomPopupPlacement[] PlaceBelowField(Size popupSize, Size targetSize, Point offset)
    {
        // Только под полем, без альтернативного положения сверху.
        return [new CustomPopupPlacement(new Point(0, targetSize.Height), PopupPrimaryAxis.Horizontal)];
    }

    private void CalendarPopup_Opened(object sender, EventArgs e)
    {
        if (SelectedDate is { } selected)
            _visibleMonth = new DateTime(selected.Year, selected.Month, 1);
        UpdateSelectors();
        RenderDays();
    }

    private void CalendarPopup_Closed(object sender, EventArgs e)
    {
        OpenButton.IsChecked = false;
    }

    private void Previous_Click(object sender, RoutedEventArgs e) => ChangeMonth(-1);
    private void Next_Click(object sender, RoutedEventArgs e) => ChangeMonth(1);

    private void ChangeMonth(int offset)
    {
        if (_visibleMonth.Year == 1 && offset < 0 ||
            _visibleMonth.Year == 9999 && offset > 0) return;
        _visibleMonth = _visibleMonth.AddMonths(offset);
        UpdateSelectors();
        RenderDays();
    }

    private void MonthSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) => SelectMonth();
    private void YearSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) => SelectMonth();

    private void SelectMonth()
    {
        if (_updatingSelectors || MonthSelector.SelectedIndex < 0 ||
            YearSelector.SelectedItem is not int year) return;
        _visibleMonth = new DateTime(year, MonthSelector.SelectedIndex + 1, 1);
        RenderDays();
    }

    private void UpdateSelectors()
    {
        if (MonthSelector is null || YearSelector is null) return;
        _updatingSelectors = true;
        MonthSelector.SelectedIndex = _visibleMonth.Month - 1;
        YearSelector.SelectedItem = _visibleMonth.Year;
        _updatingSelectors = false;
    }

    private void Today_Click(object sender, RoutedEventArgs e)
    {
        SelectedDate = DateTime.Today;
        CalendarPopup.IsOpen = false;
    }

    private void RenderDays()
    {
        if (DaysGrid is null) return;
        DaysGrid.Children.Clear();
        int offset = ((int)_visibleMonth.DayOfWeek + 6) % 7;
        DateTime start = _visibleMonth.AddDays(-offset);
        for (int i = 0; i < 42; i++)
        {
            DateTime date = start.AddDays(i);
            bool current = date.Month == _visibleMonth.Month;
            bool selected = SelectedDate?.Date == date.Date;
            var button = new Button
            {
                Content = date.Day.ToString(Russian),
                Width = 38,
                Height = 34,
                Margin = new Thickness(1),
                Padding = new Thickness(0),
                FontSize = 13,
                Foreground = (Brush)FindResource(current ? "TextBrush" : "MutedBrush"),
                Background = (Brush)FindResource(selected ? "AccentBrush" : "PanelBrush"),
                BorderThickness = new Thickness(0),
                Opacity = current ? 1 : 0.55,
                Tag = date
            };
            button.Click += Day_Click;
            DaysGrid.Children.Add(button);
        }
    }

    private void Day_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DateTime date }) return;
        SelectedDate = date;
        CalendarPopup.IsOpen = false;
    }
}
