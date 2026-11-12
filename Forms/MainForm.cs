using System.ComponentModel;
using StudentPassportMonitor.Core;
using StudentPassportMonitor.Models;
using StudentPassportMonitor.Services;

namespace StudentPassportMonitor.Forms;

public partial class MainForm : Form
{
    private StudentMonitorController? _controller;
    private readonly System.Windows.Forms.Timer _dailyTimer = new() { Interval = 60_000 };
    private DateOnly _observedDate = DateOnly.FromDateTime(DateTime.Today);
    private bool _allowExit;
    private bool _loadingGroups;
    private StudentSortOrder _sortOrder = StudentSortOrder.Name;

    public MainForm()
    {
        InitializeComponent();

        // Never access files, registry or timers while Visual Studio renders the form.
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        _controller = new StudentMonitorController(
            new JsonDatabaseService(),
            new ExcelImportService(),
            new PassportExpirationService(),
            new StudentMergeService(),
            new StartupService());

        nudWarningDays.Value = Math.Clamp(_controller.Settings.WarningDays,
            (int)nudWarningDays.Minimum, (int)nudWarningDays.Maximum);
        chkStartup.Checked = _controller.StartupEnabled;
        trayIcon.Visible = true;

        colDue.SortMode = DataGridViewColumnSortMode.Programmatic;
        dgvStudents.ColumnHeaderMouseClick += dgvStudents_ColumnHeaderMouseClick;
        RefreshView();
        _dailyTimer.Tick += DailyTimer_Tick;
        _dailyTimer.Start();
        Shown += (_, _) => CheckDaily();
    }

    private void RefreshView()
    {
        RebuildGroups();
        RenderDashboard();
        RenderStudents();
    }

    private void RenderDashboard()
    {
        if (_controller is null) return;

        DashboardSummary summary = _controller.GetSummary(DateTime.Today);
        lblTotalValue.Text = summary.Total.ToString();
        lblUpcomingValue.Text = summary.Upcoming.ToString();
        lblReviewValue.Text = summary.ReviewRequired.ToString();
        lblLastCheck.Text = summary.LastChecked is { } last
            ? $"Последняя проверка: {last:dd.MM.yyyy}"
            : "Проверка ещё не выполнялась";
    }

    private void RenderStudents()
    {
        if (_controller is null) return;

        Guid? selectedId = (dgvStudents.CurrentRow?.Tag as Student)?.Id;
        dgvStudents.Rows.Clear();

        foreach (StudentRow row in _controller.GetRows(
            DateTime.Today, txtSearch.Text.Trim(),
            cmbGroup.SelectedItem?.ToString() ?? "Все группы",
            chkAttentionOnly.Checked, _sortOrder))
        {
            int index = dgvStudents.Rows.Add(
                row.FullName, row.Group, row.Faculty, row.BirthDate,
                row.PassportNumber, row.Age, row.DueDate, row.State);

            DataGridViewRow gridRow = dgvStudents.Rows[index];
            gridRow.Tag = row.Student;
            gridRow.DefaultCellStyle.BackColor = row.AlertStatus switch
            {
                PassportAlertState.ReviewRequired => Color.MistyRose,
                PassportAlertState.Upcoming => Color.FromArgb(255, 224, 178),
                _ => Color.White
            };
        }

        if (selectedId is { } id)
        {
            foreach (DataGridViewRow row in dgvStudents.Rows)
            {
                if ((row.Tag as Student)?.Id != id) continue;
                row.Selected = true;
                dgvStudents.CurrentCell = row.Cells[0];
                break;
            }
        }
        colDue.HeaderCell.SortGlyphDirection = _sortOrder switch
        {
            StudentSortOrder.DueDateAscending => SortOrder.Ascending,
            StudentSortOrder.DueDateDescending => SortOrder.Descending,
            _ => SortOrder.None
        };
        UpdateSelectedStudent();
    }

    private void dgvStudents_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex != colDue.Index) return;

        _sortOrder = _sortOrder == StudentSortOrder.DueDateAscending
            ? StudentSortOrder.DueDateDescending
            : StudentSortOrder.DueDateAscending;
        RenderStudents();
    }

    private void chkAttentionOnly_CheckedChanged(object? sender, EventArgs e) => RenderStudents();

    private void RebuildGroups()
    {
        if (_controller is null) return;
        string selected = cmbGroup.SelectedItem?.ToString() ?? "Все группы";

        _loadingGroups = true;
        try
        {
            cmbGroup.Items.Clear();
            cmbGroup.Items.Add("Все группы");
            foreach (string group in _controller.GetGroups())
                cmbGroup.Items.Add(group);

            cmbGroup.SelectedItem = cmbGroup.Items.Contains(selected)
                ? selected : "Все группы";
        }
        finally { _loadingGroups = false; }
    }

    private void UpdateSelectedStudent()
    {
        Student? selected = dgvStudents.CurrentRow?.Tag as Student;
        lblSelectedStudent.Text = selected?.FullName ?? "Выберите студента";
        txtReplacementDate.Text = selected?.LastPassportReplacementDate?.ToString("dd.MM.yyyy") ?? "";
    }

    private void DailyTimer_Tick(object? sender, EventArgs e)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        if (today == _observedDate) return;

        _observedDate = today;
        CheckDaily();
    }

    private void CheckDaily(bool force = false)
    {
        if (_controller is null) return;
        try
        {
            IReadOnlyList<PassportAlert> alerts = _controller.CheckDaily(DateTime.Today, force);
            // Windows balloon tips can replace each other: send a single summary.
            if (alerts.Count > 0)
                trayIcon.ShowBalloonTip(5000, "Проверка паспортов",
                    $"Требуют внимания: {alerts.Count} студентов. Откройте приложение для подробностей.",
                    ToolTipIcon.Info);

            RenderDashboard();
            RenderStudents();
            if (force)
                MessageBox.Show(this, $"Проверка завершена. Требуют внимания: {alerts.Count}.",
                    "Student Passport Monitor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError("Ошибка ежедневной проверки", ex); }
    }

    private void btnAddStudent_Click(object? sender, EventArgs e) => OpenStudentEditor(null);

    private void btnEditStudent_Click(object? sender, EventArgs e)
    {
        if (dgvStudents.CurrentRow?.Tag is not Student selected)
        {
            MessageBox.Show(this, "Выберите студента.", "Редактирование",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        OpenStudentEditor(selected);
    }

    private void dgvStudents_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && dgvStudents.Rows[e.RowIndex].Tag is Student student)
            OpenStudentEditor(student);
    }

    private void OpenStudentEditor(Student? student)
    {
        if (_controller is null) return;
        Student? draft = student;

        while (true)
        {
            using var editor = new StudentEditorForm(draft);
            if (editor.ShowDialog(this) != DialogResult.OK || editor.Result is not { } updated)
                return;

            try
            {
                _controller.SaveStudent(updated);
                RefreshView();
                return;
            }
            catch (ArgumentException ex)
            {
                ShowError("Проверьте данные студента", ex);
                draft = updated;
            }
            catch (Exception ex)
            {
                ShowError("Не удалось сохранить студента", ex);
                return;
            }
        }
    }

    private void btnDeleteStudent_Click(object? sender, EventArgs e)
    {
        if (_controller is null || dgvStudents.CurrentRow?.Tag is not Student student)
        {
            MessageBox.Show(this, "Выберите студента.", "Удаление",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show(this, $"Удалить студента «{student.FullName}»?\\nДействие нельзя отменить.",
                "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            != DialogResult.Yes) return;
        try
        {
            _controller.DeleteStudent(student.Id);
            RefreshView();
        }
        catch (Exception ex) { ShowError("Ошибка удаления", ex); }
    }

    private void btnImport_Click(object? sender, EventArgs e)
    {
        if (_controller is null) return;
        using var dialog = new OpenFileDialog
        {
            Title = "Импорт списка студентов",
            Filter = "Excel (*.xlsx;*.xls)|*.xlsx;*.xls"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var choice = MessageBox.Show(this,
            "Да — заменить текущий список.\nНет — объединить с текущим списком.",
            "Импорт Excel", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (choice == DialogResult.Cancel) return;

        try
        {
            int count = _controller.Import(dialog.FileName, choice == DialogResult.Yes);
            RefreshView();
            MessageBox.Show(this, $"Обработано записей: {count}.",
                "Импорт завершён", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError("Ошибка импорта", ex); }
    }

    private void btnSaveSettings_Click(object? sender, EventArgs e)
    {
        if (_controller is null) return;
        try
        {
            _controller.SaveSettings((int)nudWarningDays.Value, chkStartup.Checked);
            RefreshView();
            MessageBox.Show(this, "Настройки сохранены.", "Настройки",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError("Ошибка сохранения настроек", ex); }
    }

    private void btnSaveReplacement_Click(object? sender, EventArgs e)
    {
        if (_controller is null) return;
        if (dgvStudents.CurrentRow?.Tag is not Student student)
        {
            MessageBox.Show(this, "Сначала выберите студента.", "Нет выбора",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _controller.SaveReplacement(student.Id, txtReplacementDate.Text);
            RenderDashboard();
            RenderStudents();
        }
        catch (Exception ex) { ShowError("Ошибка сохранения даты", ex); }
    }

    private void btnCheckNow_Click(object? sender, EventArgs e) => CheckDaily(force: true);
    private void btnRefresh_Click(object? sender, EventArgs e) => RefreshView();
    private void txtSearch_TextChanged(object? sender, EventArgs e) => RenderStudents();
    private void cmbGroup_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_loadingGroups) RenderStudents();
    }
    private void dgvStudents_SelectionChanged(object? sender, EventArgs e) => UpdateSelectedStudent();

    private void trayOpen_Click(object? sender, EventArgs e)
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void trayExit_Click(object? sender, EventArgs e)
    {
        _allowExit = true;
        Close();
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
        _dailyTimer.Dispose();
        trayIcon.Visible = false;
        base.OnFormClosing(e);
    }

    private void ShowError(string title, Exception ex) =>
        MessageBox.Show(this, ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
}
