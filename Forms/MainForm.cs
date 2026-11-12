using System.Globalization;
using StudentPassportMonitor.Models;
using StudentPassportMonitor.Services;

namespace StudentPassportMonitor.Forms;

public partial class MainForm : Form
{
    private readonly JsonDatabaseService _db = new();
    private readonly ExcelImportService _excel = new();
    private readonly PassportExpirationService _passport = new();
    private readonly StartupService _startup = new();
    private readonly StudentMergeService _merger = new();
    private readonly System.Windows.Forms.Timer _dailyTimer = new();
    private List<Student> _students = new();
    private AppSettings _settings = new();
    private bool _allowExit;
    private DateOnly _lastObservedDate = DateOnly.FromDateTime(DateTime.Today);

    public MainForm()
    {
        InitializeComponent();
        _students = _db.LoadStudents();
        _settings = _db.LoadSettings();
        nudWarningDays.Value = Math.Clamp(_settings.WarningDays, (int)nudWarningDays.Minimum, (int)nudWarningDays.Maximum);
        chkStartup.Checked = _startup.IsEnabled();
        _settings.StartWithWindows = chkStartup.Checked;

        trayIcon.Visible = true;
        RebuildGroups();
        RefreshDashboard();
        _dailyTimer.Interval = 60_000;
        _dailyTimer.Tick += DailyTimer_Tick;
        _dailyTimer.Start();
        Shown += (_, _) => RunDailyCheck();
    }

    private void DailyTimer_Tick(object? sender, EventArgs e)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        if (today == _lastObservedDate) return;

        _lastObservedDate = today;
        RunDailyCheck();
    }

    private void RefreshDashboard()
    {
        var alerts = _passport.GetAlerts(_students, DateTime.Today, _settings.WarningDays);
        lblTotalValue.Text = _students.Count.ToString();
        lblUpcomingValue.Text = alerts.Count(a => a.DaysRemaining >= 0 && a.DaysRemaining <= _settings.WarningDays).ToString();
        lblReviewValue.Text = alerts.Count(a => a.Status == PassportAlertState.ReviewRequired).ToString();

        dgvStudents.Rows.Clear();
        foreach (var student in _students.OrderBy(s => s.LastName).ThenBy(s => s.FirstName))
        {
            var alert = _passport.GetAlert(student, DateTime.Today, _settings.WarningDays);
            int i = dgvStudents.Rows.Add(student.FullName, student.Group, student.Faculty,
                student.BirthDate?.ToString("dd.MM.yyyy") ?? "—",
                student.PassportNumber, alert?.MilestoneAge.ToString() ?? "—",
                alert?.DueDate.ToString("dd.MM.yyyy") ?? "—", alert?.State ?? "Нет даты рождения");
            dgvStudents.Rows[i].Tag = student;
            if (alert?.Status == PassportAlertState.ReviewRequired)
                dgvStudents.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
            else if (alert?.Status == PassportAlertState.Upcoming)
                dgvStudents.Rows[i].DefaultCellStyle.BackColor = Color.LemonChiffon;
        }
        lblLastCheck.Text = _settings.LastDailyCheckDate is DateTime last
            ? $"Последняя проверка: {last:dd.MM.yyyy}" : "Проверка ещё не выполнялась";
        ApplyFilter();
    }

    private void RunDailyCheck(bool force = false)
    {
        if (!force && _settings.LastDailyCheckDate?.Date == DateTime.Today) return;
        var alerts = _passport.GetAlerts(_students, DateTime.Today, _settings.WarningDays)
            .Where(a => a.DaysRemaining <= _settings.WarningDays)
            .ToList();

        foreach (var alert in alerts)
        {
            string when = alert.DaysRemaining < 0
                ? $"контрольная дата {alert.DueDate:dd.MM.yyyy} уже прошла"
                : $"до даты {alert.DueDate:dd.MM.yyyy} осталось {alert.DaysRemaining} дн.";
            trayIcon.ShowBalloonTip(5000, "Проверка паспорта", $"{alert.Student.FullName}: рубеж {alert.MilestoneAge} лет — {when}. Проверьте документ.", ToolTipIcon.Info);
        }

        _settings.LastDailyCheckDate = DateTime.Today;
        _db.SaveSettings(_settings);
        RefreshDashboard();
        if (force)
            MessageBox.Show($"Проверка завершена. Требуют внимания: {alerts.Count}.", "Student Passport Monitor",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void btnImport_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Импорт списка студентов",
            Filter = "Excel (*.xlsx;*.xls)|*.xlsx;*.xls",
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var imported = _excel.Import(dialog.FileName);
            var answer = MessageBox.Show(
                $"Найдено записей: {imported.Count}.\nДа — заменить текущий список.\nНет — добавить записи к текущему списку.",
                "Импорт Excel", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (answer == DialogResult.Cancel) return;
            _students = answer == DialogResult.Yes
                ? imported
                : _merger.Merge(_students, imported);
            _db.SaveStudents(_students);
            RebuildGroups();
            RefreshDashboard();
            ApplyFilter();
            MessageBox.Show($"Импортировано записей: {imported.Count}.", "Готово",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось импортировать файл.\n{ex.Message}", "Ошибка импорта",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

private void btnSaveSettings_Click(object? sender, EventArgs e)
    {
        _settings.WarningDays = (int)nudWarningDays.Value;
        try
        {
            _startup.SetEnabled(chkStartup.Checked);
            _settings.StartWithWindows = chkStartup.Checked;
            _db.SaveSettings(_settings);
            RefreshDashboard();
            MessageBox.Show("Настройки сохранены.", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось сохранить настройки автозапуска: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnCheckNow_Click(object? sender, EventArgs e) => RunDailyCheck(force: true);

    private void dgvStudents_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvStudents.CurrentRow?.Tag is not Student student) return;
        txtReplacementDate.Text = student.LastPassportReplacementDate?.ToString("dd.MM.yyyy") ?? "";
        lblSelectedStudent.Text = student.FullName;
    }

    private void btnSaveReplacement_Click(object? sender, EventArgs e)
    {
        if (dgvStudents.CurrentRow?.Tag is not Student student)
        {
            MessageBox.Show("Сначала выберите студента.", "Нет выбора", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(txtReplacementDate.Text))
            student.LastPassportReplacementDate = null;
        else if (DateTime.TryParseExact(
            txtReplacementDate.Text.Trim(),
            "dd.MM.yyyy",
            CultureInfo.GetCultureInfo("ru-RU"),
            DateTimeStyles.None,
            out var date) && date.Date <= DateTime.Today &&
            (student.BirthDate is null || date.Date >= student.BirthDate.Value.Date))
            student.LastPassportReplacementDate = date.Date;
        else
        {
            MessageBox.Show("Введите корректную дату ДД.ММ.ГГГГ (не в будущем и не раньше рождения).", "Некорректная дата",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _db.SaveStudents(_students);
        RefreshDashboard();
    }

    private void txtSearch_TextChanged(object? sender, EventArgs e) => ApplyFilter();
    private void cmbGroup_SelectedIndexChanged(object? sender, EventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        string query = txtSearch.Text.Trim();
        string group = cmbGroup.SelectedItem?.ToString() ?? "Все группы";
        dgvStudents.CurrentCell = null;
        foreach (DataGridViewRow row in dgvStudents.Rows)
        {
            if (row.Tag is not Student student) continue;

            row.Visible =
                (string.IsNullOrWhiteSpace(query) ||
                 student.FullName.Contains(query, StringComparison.CurrentCultureIgnoreCase)) &&
                (group == "Все группы" ||
                 student.Group.Equals(group, StringComparison.CurrentCultureIgnoreCase));
        }
    }

    private void RebuildGroups()
    {
        var selected = cmbGroup.SelectedItem?.ToString() ?? "Все группы";
        cmbGroup.Items.Clear();
        cmbGroup.Items.Add("Все группы");
        foreach (var group in _students.Select(s => s.Group).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s))
            cmbGroup.Items.Add(group);
        cmbGroup.SelectedItem = cmbGroup.Items.Contains(selected) ? selected : "Все группы";
    }

    private void btnRefresh_Click(object? sender, EventArgs e)
    {
        RebuildGroups();
        RefreshDashboard();
        ApplyFilter();
    }

    private void trayOpen_Click(object? sender, EventArgs e)
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void trayExit_Click(object? sender, EventArgs e)
    {
        _allowExit = true;
        trayIcon.Visible = false;
        _dailyTimer.Stop();
        Application.Exit();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            trayIcon.ShowBalloonTip(1500, "Student Passport Monitor",
                "Приложение продолжает работать в системном трее.", ToolTipIcon.Info);
            return;
        }
        _dailyTimer.Stop();
        trayIcon.Visible = false;
        base.OnFormClosing(e);
    }
}