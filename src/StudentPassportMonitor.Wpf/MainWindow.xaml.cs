using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using StudentPassportMonitor.Core;
using StudentPassportMonitor.Models;
using StudentPassportMonitor.Services;

namespace StudentPassportMonitor.Wpf;

public partial class MainWindow : Window
{
    private readonly StudentMonitorController _controller;
    private readonly ObservableCollection<StudentRow> _students = [];
    private readonly ObservableCollection<AttentionItem> _attention = [];
    private readonly ObservableCollection<HistoryItem> _history = [];
    private readonly ObservableCollection<AttentionItem> _dashboardAttention = [];
    private readonly System.Windows.Threading.DispatcherTimer _timer;
    private DateTime _lastObservedDate = DateTime.Today;

    public MainWindow()
    {
        InitializeComponent();
        _controller = new StudentMonitorController(new JsonDatabaseService(),
            new ExcelImportService(), new PassportExpirationService(),
            new StudentMergeService(), new StartupService());
        StudentsGrid.ItemsSource = _students;
        AttentionGrid.ItemsSource = _attention;
        DashboardAttentionGrid.ItemsSource = _dashboardAttention;
        HistoryGrid.ItemsSource = _history;
        WarningDaysBox.Text = _controller.Settings.WarningDays.ToString(CultureInfo.InvariantCulture);
        StartupCheck.IsChecked = _controller.StartupEnabled;
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        _timer.Tick += (_, _) =>
        {
            if (_lastObservedDate == DateTime.Today) return;
            _lastObservedDate = DateTime.Today;
            CheckDaily(false);
        };
        _timer.Start();
        RefreshAll();
        CheckDaily(false);
    }

    private void ShowPage(Grid page, string title)
    {
        foreach (Grid item in new[] { DashboardPage, StudentsPage, NotificationsPage,
                                      ReportsPage, HistoryPage, SettingsPage })
            item.Visibility = item == page ? Visibility.Visible : Visibility.Collapsed;
        PageTitle.Text = title;
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e) => ShowPage(DashboardPage, "Обзор");
    private void Students_Click(object sender, RoutedEventArgs e) => ShowPage(StudentsPage, "Студенты");

    private void DashboardAll_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        AttentionFilter.IsChecked = false;
        ShowPage(StudentsPage, "Студенты");
    }

    private void DashboardAttention_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        ShowPage(NotificationsPage, "Уведомления");

    private void DashboardAttentionButton_Click(object sender, RoutedEventArgs e) =>
        ShowPage(NotificationsPage, "Уведомления");

    private void DashboardAttentionGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DashboardAttentionGrid.SelectedItem is AttentionItem item)
            EditStudent(item.Student);
    }
    private void Notifications_Click(object sender, RoutedEventArgs e) => ShowPage(NotificationsPage, "Уведомления");
    private void Reports_Click(object sender, RoutedEventArgs e) => ShowPage(ReportsPage, "Отчёты");
    private void History_Click(object sender, RoutedEventArgs e) => ShowPage(HistoryPage, "История проверок");
    private void Settings_Click(object sender, RoutedEventArgs e) => ShowPage(SettingsPage, "Настройки");

    private void RefreshAll()
    {
        DashboardSummary summary = _controller.GetSummary(DateTime.Today);
        TotalValue.Text = summary.Total.ToString();
        UpcomingValue.Text = summary.Upcoming.ToString();
        ReviewValue.Text = summary.ReviewRequired.ToString();
        LastCheckValue.Text = summary.LastChecked is { } last
            ? $"Последняя проверка: {last:dd.MM.yyyy}"
            : "Ежедневная проверка ещё не выполнялась";
        FooterStatus.Text = $"База загружена  |  Студентов: {summary.Total}  |  Требуют внимания: {summary.Upcoming + summary.ReviewRequired}";

        string selectedGroup = GroupFilter.SelectedItem?.ToString() ?? "Все группы";
        string reportGroup = ReportGroup.SelectedItem?.ToString() ?? "";
        GroupFilter.Items.Clear();
        GroupFilter.Items.Add("Все группы");
        ReportGroup.Items.Clear();
        foreach (string group in _controller.GetGroups())
        {
            GroupFilter.Items.Add(group);
            ReportGroup.Items.Add(group);
        }
        GroupFilter.SelectedItem = GroupFilter.Items.Contains(selectedGroup)
            ? selectedGroup : "Все группы";
        if (ReportGroup.Items.Contains(reportGroup)) ReportGroup.SelectedItem = reportGroup;
        else if (ReportGroup.Items.Count > 0) ReportGroup.SelectedIndex = 0;

        RefreshStudents();
        _attention.Clear();
        _dashboardAttention.Clear();
        foreach (PassportAlert alert in _controller.GetAttention(DateTime.Today))
        {
            var item = new AttentionItem(alert.Student, alert.DueDate.ToString("dd.MM.yyyy"),
                alert.State, alert.Student.LastReviewedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Не проверялся");
            _attention.Add(item);
            if (_dashboardAttention.Count < 8)
                _dashboardAttention.Add(item);
        }
        DashboardAttentionSummary.Text = _attention.Count == 0
            ? "Нет событий, требующих внимания"
            : $"Требуют внимания: {_attention.Count}. Показаны первые {_dashboardAttention.Count}.";

        _history.Clear();
        foreach (CheckHistoryEntry item in _controller.GetHistory())
            _history.Add(new HistoryItem(item.CheckedAt.ToString("dd.MM.yyyy HH:mm"),
                item.Manual ? "Вручную" : "Автоматически", item.AttentionCount));
    }

    private void RefreshStudents()
    {
        if (_controller is null || GroupFilter is null || StudentsGrid is null) return;
        Guid? selected = (StudentsGrid.SelectedItem as StudentRow)?.Student.Id;
        var rows = _controller.GetRows(DateTime.Today,
            SearchBox?.Text ?? "", GroupFilter.SelectedItem?.ToString() ?? "Все группы",
            AttentionFilter?.IsChecked == true);
        _students.Clear();
        foreach (StudentRow row in rows) _students.Add(row);
        if (selected is { } id)
            StudentsGrid.SelectedItem = _students.FirstOrDefault(s => s.Student.Id == id);
    }

    private void Filter_Changed(object sender, RoutedEventArgs e) => RefreshStudents();
    private void Filter_Changed(object sender, TextChangedEventArgs e) => RefreshStudents();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => RefreshStudents();
    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshAll();

    private void CheckDaily(bool force)
    {
        try
        {
            IReadOnlyList<PassportAlert> alerts = _controller.CheckDaily(DateTime.Today, force);
            RefreshAll();
            if (force)
                MessageBox.Show(this, $"Проверка завершена. Требуют внимания: {alerts.Count}.",
                    "Мониторинг", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void CheckNow_Click(object sender, RoutedEventArgs e) => CheckDaily(true);

    private void AddStudent_Click(object sender, RoutedEventArgs e) => EditStudent(null);
    private void EditStudent_Click(object sender, RoutedEventArgs e)
    {
        if (StudentsGrid.SelectedItem is StudentRow row) EditStudent(row.Student);
        else MessageBox.Show(this, "Сначала выберите студента.");
    }
    private void StudentsGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (StudentsGrid.SelectedItem is StudentRow row) EditStudent(row.Student);
    }
    private void OpenAttention_Click(object sender, RoutedEventArgs e)
    {
        if (AttentionGrid.SelectedItem is AttentionItem item) EditStudent(item.Student);
        else MessageBox.Show(this, "Сначала выберите студента.");
    }
    private void AttentionGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (AttentionGrid.SelectedItem is AttentionItem item) EditStudent(item.Student);
    }

    private void EditStudent(Student? original)
    {
        var editor = new StudentEditorWindow(original) { Owner = this };
        if (editor.ShowDialog() != true || editor.Result is not { } result) return;
        try
        {
            _controller.SaveStudent(result);
            RefreshAll();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void DeleteStudent_Click(object sender, RoutedEventArgs e)
    {
        if (StudentsGrid.SelectedItem is not StudentRow row) { MessageBox.Show(this, "Выберите студента."); return; }
        if (MessageBox.Show(this, $"Удалить запись «{row.FullName}»?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { _controller.DeleteStudent(row.Student.Id); RefreshAll(); }
        catch (Exception ex) { ShowError(ex); }
    }

    private void MarkReviewed_Click(object sender, RoutedEventArgs e)
    {
        if (AttentionGrid.SelectedItem is not AttentionItem item) { MessageBox.Show(this, "Выберите студента."); return; }
        if (MessageBox.Show(this, "Отметить сведения проверенными? Это не подтверждает замену паспорта.",
            "Проверка", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        try { _controller.MarkReviewed(item.Student.Id); RefreshAll(); }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Excel (*.xlsx;*.xls)|*.xlsx;*.xls" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var preview = _controller.PreviewImport(dialog.FileName);
            if (preview.Students.Count == 0) { MessageBox.Show(this, "В файле нет студентов."); return; }
            var answer = MessageBox.Show(this,
                $"Найдено студентов: {preview.Students.Count}\nБез даты рождения: {preview.MissingBirthDates}\n" +
                $"Без номера паспорта: {preview.MissingPassportNumbers}\n\nДа — заменить список. Нет — объединить.",
                "Предпросмотр импорта", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Cancel) return;
            _controller.Import(preview, answer == MessageBoxResult.Yes);
            RefreshAll();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Книга Excel (*.xlsx)|*.xlsx",
            FileName = $"passport-report-{DateTime.Today:yyyy-MM-dd}.xlsx"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var type = (ExcelReportType)Math.Max(0, ReportType.SelectedIndex);
            int count = _controller.ExportReport(dialog.FileName, type, ReportGroup.SelectedItem?.ToString());
            MessageBox.Show(this, $"Отчёт сохранён. Строк: {count}.");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(WarningDaysBox.Text, out int days) || days is < 1 or > 730)
        {
            MessageBox.Show(this, "Укажите количество дней от 1 до 730."); return;
        }
        try
        {
            _controller.SaveSettings(days, StartupCheck.IsChecked == true);
            RefreshAll();
            MessageBox.Show(this, "Настройки сохранены.");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        var backups = _controller.ListBackups();
        if (backups.Count == 0) { MessageBox.Show(this, "Резервных копий нет."); return; }
        var picker = new Window
        {
            Title = "Восстановление базы", Width = 640, Height = 380,
            Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = System.Windows.Media.Brushes.White
        };
        var panel = new DockPanel { Margin = new Thickness(16) };
        var list = new ListBox { ItemsSource = backups };
        var restore = new Button { Content = "Восстановить", Height = 42, Margin = new Thickness(0, 12, 0, 0) };
        DockPanel.SetDock(restore, Dock.Bottom);
        panel.Children.Add(restore);
        panel.Children.Add(list);
        picker.Content = panel;
        restore.Click += (_, _) => { if (list.SelectedItem is not null) picker.DialogResult = true; };
        if (picker.ShowDialog() != true || list.SelectedItem is not string path) return;
        if (MessageBox.Show(this, "Восстановить выбранную копию? Текущая база будет сохранена в резервную копию.",
            "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        try { _controller.RestoreBackup(path); RefreshAll(); }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ShowError(Exception ex) =>
        MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

    private sealed record AttentionItem(Student Student, string DueDate, string State, string Reviewed)
    {
        public string FullName => Student.FullName;
        public string Group => Student.Group;
    }

    private sealed record HistoryItem(string CheckedAt, string Mode, int Count);
}
