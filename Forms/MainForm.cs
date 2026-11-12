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
        RenderNotifications();
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
                PassportAlertState.ConfirmationRequired => Color.FromArgb(255, 224, 178),
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

    private void RenderNotifications()
    {
        if (_controller is null) return;
        Guid? selectedId = (dgvNotifications.CurrentRow?.Tag as Student)?.Id;
        dgvNotifications.Rows.Clear();

        foreach (PassportAlert alert in _controller.GetAttention(DateTime.Today))
        {
            Student student = alert.Student;
            int rowIndex = dgvNotifications.Rows.Add(
                student.FullName, student.Group,
                alert.DueDate.ToString("dd.MM.yyyy"), alert.State,
                student.LastReviewedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Не проверялся");
            var row = dgvNotifications.Rows[rowIndex];
            row.Tag = student;
            row.DefaultCellStyle.BackColor =
                alert.Status == PassportAlertState.ReviewRequired
                    ? Color.MistyRose : Color.FromArgb(255, 224, 178);
            if (student.Id == selectedId)
                row.Selected = true;
        }

        lstCheckHistory.Items.Clear();
        lstCheckHistory.Items.Add("История запусков проверки:");
        foreach (CheckHistoryEntry entry in _controller.GetHistory())
            lstCheckHistory.Items.Add(
                $"{entry.CheckedAt:dd.MM.yyyy HH:mm} — " +
                $"{(entry.Manual ? "Вручную" : "Автоматически")}, " +
                $"требуют внимания: {entry.AttentionCount}");
    }

    private void btnRefreshAttention_Click(object? sender, EventArgs e) => RenderNotifications();

    private void btnOpenAttentionStudent_Click(object? sender, EventArgs e)
    {
        if (dgvNotifications.CurrentRow?.Tag is Student student)
            OpenStudentEditor(student);
        else
            MessageBox.Show(this, "Выберите студента из списка.", "Уведомления",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void dgvNotifications_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && dgvNotifications.Rows[e.RowIndex].Tag is Student student)
            OpenStudentEditor(student);
    }

    private void btnMarkReviewed_Click(object? sender, EventArgs e)
    {
        if (_controller is null || dgvNotifications.CurrentRow?.Tag is not Student student)
        {
            MessageBox.Show(this, "Выберите студента из списка.", "Уведомления",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show(this,
                $"Отметить проверку сведений студента «{student.FullName}»?\n" +
                "Это не подтверждает замену паспорта и не убирает предупреждение.",
                "Подтверждение проверки", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            _controller.MarkReviewed(student.Id);
            RenderNotifications();
        }
        catch (Exception ex) { ShowError("Не удалось отметить проверку", ex); }
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
            RenderNotifications();
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
        if (MessageBox.Show(this, $"Удалить студента «{student.FullName}»?\nДействие нельзя отменить.",
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

        try
        {
            StudentMonitorController.ImportPreview preview = _controller.PreviewImport(dialog.FileName);
            string examples = string.Join("\n", preview.Students.Take(3)
                .Select(s => $"• {s.FullName} — {s.Group}"));
            var choice = MessageBox.Show(this,
                $"Найдено студентов: {preview.Students.Count}\n" +
                $"Без даты рождения: {preview.MissingBirthDates}\n" +
                $"Без номера паспорта: {preview.MissingPassportNumbers}\n\n" +
                $"Примеры:\n{examples}\n\n" +
                "Да — ЗАМЕНИТЬ весь список (внесённые вручную сведения могут быть утрачены).\n" +
                "Нет — ОБЪЕДИНИТЬ, сохранив имеющиеся данные.\n" +
                "Отмена — ничего не изменять.\n\n" +
                "Перед импортом автоматически создаётся резервная копия.",
                "Предпросмотр импорта Excel", MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
            if (choice == DialogResult.Cancel) return;

            int count = _controller.Import(preview, choice == DialogResult.Yes);
            RefreshView();
            MessageBox.Show(this, $"Импортировано записей: {count}.\nРезервная копия создана.",
                "Импорт завершён", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError("Ошибка импорта", ex); }
    }

    private void btnRestoreBackup_Click(object? sender, EventArgs e)
    {
        if (_controller is null) return;
        try
        {
            var backups = _controller.ListBackups();
            if (backups.Count == 0)
            {
                MessageBox.Show(this, "Резервных копий пока нет.", "Восстановление",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var picker = new Form
            {
                Text = "Выбор резервной копии",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(600, 350),
                MinimumSize = new Size(450, 280)
            };
            var list = new ListBox { Dock = DockStyle.Fill };
            foreach (string backup in backups)
                list.Items.Add(backup);
            list.SelectedIndex = 0;
            var restore = new Button { Text = "Восстановить", Dock = DockStyle.Bottom, Height = 42 };
            restore.Click += (_, _) => { picker.DialogResult = DialogResult.OK; picker.Close(); };
            picker.Controls.Add(list);
            picker.Controls.Add(restore);

            if (picker.ShowDialog(this) != DialogResult.OK ||
                list.SelectedItem is not string selected)
                return;

            if (MessageBox.Show(this,
                    "Восстановить выбранную копию? Текущие данные будут предварительно сохранены.",
                    "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                != DialogResult.Yes)
                return;

            _controller.RestoreBackup(selected);
            RefreshView();
            MessageBox.Show(this, "База восстановлена.", "Восстановление",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError("Ошибка восстановления", ex); }
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
