#nullable enable
namespace StudentPassportMonitor.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;
    private TabControl tabs = null!;
    private TabPage tabDashboard = null!;
    private TabPage tabStudents = null!;
    private TabPage tabSettings = null!;
    private Label lblTotalValue = null!;
    private Label lblUpcomingValue = null!;
    private Label lblReviewValue = null!;
    private Label lblLastCheck = null!;
    private DataGridView dgvStudents = null!;
    private Button btnImport = null!;
    private Button btnRefresh = null!;
    private TextBox txtSearch = null!;
    private ComboBox cmbGroup = null!;
    private NumericUpDown nudWarningDays = null!;
    private CheckBox chkStartup = null!;
    private Button btnSaveSettings = null!;
    private Button btnCheckNow = null!;
    private TextBox txtReplacementDate = null!;
    private Button btnSaveReplacement = null!;
    private Label lblSelectedStudent = null!;
    private NotifyIcon trayIcon = null!;
    private ContextMenuStrip trayMenu = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        tabs = new TabControl();
        tabDashboard = new TabPage("Обзор");
        tabStudents = new TabPage("Студенты");
        tabSettings = new TabPage("Настройки");
        lblTotalValue = new Label();
        lblUpcomingValue = new Label();
        lblReviewValue = new Label();
        lblLastCheck = new Label();
        dgvStudents = new DataGridView();
        btnImport = new Button();
        btnRefresh = new Button();
        txtSearch = new TextBox();
        cmbGroup = new ComboBox();
        nudWarningDays = new NumericUpDown();
        chkStartup = new CheckBox();
        btnSaveSettings = new Button();
        btnCheckNow = new Button();
        txtReplacementDate = new TextBox();
        btnSaveReplacement = new Button();
        lblSelectedStudent = new Label();
        trayIcon = new NotifyIcon(components);
        trayMenu = new ContextMenuStrip(components);
        var trayOpen = new ToolStripMenuItem("Открыть");
        var trayExit = new ToolStripMenuItem("Выход");

        SuspendLayout();
        Text = "Student Passport Monitor";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        Size = new Size(1180, 760);
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(245, 247, 250);

        tabs.Dock = DockStyle.Fill;
        tabs.Controls.AddRange(new Control[] { tabDashboard, tabStudents, tabSettings });
        tabs.Padding = new Point(16, 6);
        Controls.Add(tabs);

        tabDashboard.BackColor = Color.White;
        var dashboardTitle = new Label { Text = "Панель мониторинга", Font = new Font("Segoe UI", 20F, FontStyle.Bold), AutoSize = true, Location = new Point(28, 24) };
        var card1 = MakeCard("Всего студентов", lblTotalValue, 30);
        var card2 = MakeCard("Скоро контрольная дата", lblUpcomingValue, 300);
        var card3 = MakeCard("Требуют проверки", lblReviewValue, 570);
        lblLastCheck.Location = new Point(34, 220);
        lblLastCheck.AutoSize = true;
        lblLastCheck.ForeColor = Color.DimGray;
        btnCheckNow.Text = "Проверить сейчас";
        btnCheckNow.Location = new Point(30, 270);
        btnCheckNow.Size = new Size(170, 38);
        btnCheckNow.Click += btnCheckNow_Click;
        tabDashboard.Controls.AddRange(new Control[] { dashboardTitle, card1, card2, card3, lblLastCheck, btnCheckNow });

        tabStudents.BackColor = Color.White;
        btnImport.Text = "Импорт Excel";
        btnImport.Location = new Point(22, 18);
        btnImport.Size = new Size(130, 34);
        btnImport.Click += btnImport_Click;
        btnRefresh.Text = "Обновить";
        btnRefresh.Location = new Point(160, 18);
        btnRefresh.Size = new Size(100, 34);
        btnRefresh.Click += btnRefresh_Click;
        txtSearch.PlaceholderText = "Поиск по ФИО...";
        txtSearch.Location = new Point(280, 20);
        txtSearch.Size = new Size(240, 30);
        txtSearch.TextChanged += txtSearch_TextChanged;
        cmbGroup.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbGroup.Location = new Point(535, 20);
        cmbGroup.Size = new Size(180, 30);
        cmbGroup.Items.Add("Все группы");
        cmbGroup.SelectedIndex = 0;
        cmbGroup.SelectedIndexChanged += cmbGroup_SelectedIndexChanged;

        dgvStudents.Location = new Point(22, 66);
        dgvStudents.Size = new Size(1090, 500);
        dgvStudents.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgvStudents.AllowUserToAddRows = false;
        dgvStudents.AllowUserToDeleteRows = false;
        dgvStudents.ReadOnly = true;
        dgvStudents.MultiSelect = false;
        dgvStudents.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvStudents.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvStudents.RowHeadersVisible = false;
        dgvStudents.BackgroundColor = Color.White;
        dgvStudents.BorderStyle = BorderStyle.None;
        dgvStudents.SelectionChanged += dgvStudents_SelectionChanged;
        foreach (var header in new[] { "ФИО", "Группа", "Факультет", "Дата рождения", "Паспорт №", "Возраст", "Контрольная дата", "Состояние" })
            dgvStudents.Columns.Add(header, header);
        dgvStudents.Columns[0].FillWeight = 155;
        dgvStudents.Columns[4].FillWeight = 90;

        var replacementPanel = new Panel { Location = new Point(22, 578), Size = new Size(1090, 55), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.FromArgb(246, 248, 251) };
        lblSelectedStudent.Text = "Выберите студента";
        lblSelectedStudent.Location = new Point(12, 17);
        lblSelectedStudent.AutoSize = true;
        txtReplacementDate.PlaceholderText = "ДД.ММ.ГГГГ";
        txtReplacementDate.Location = new Point(500, 12);
        txtReplacementDate.Size = new Size(130, 28);
        btnSaveReplacement.Text = "Сохранить дату замены";
        btnSaveReplacement.Location = new Point(645, 10);
        btnSaveReplacement.Size = new Size(190, 32);
        btnSaveReplacement.Click += btnSaveReplacement_Click;
        replacementPanel.Controls.AddRange(new Control[] { lblSelectedStudent, txtReplacementDate, btnSaveReplacement });
        tabStudents.Controls.AddRange(new Control[] { btnImport, btnRefresh, txtSearch, cmbGroup, dgvStudents, replacementPanel });

        tabSettings.BackColor = Color.White;
        var settingsTitle = new Label { Text = "Параметры мониторинга", Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Location = new Point(28, 28) };
        var warningLabel = new Label { Text = "Предупреждать за (дней):", AutoSize = true, Location = new Point(34, 100) };
        nudWarningDays.Location = new Point(250, 94);
        nudWarningDays.Minimum = 1;
        nudWarningDays.Maximum = 730;
        nudWarningDays.Value = 90;
        nudWarningDays.Size = new Size(100, 28);
        chkStartup.Text = "Запускать вместе с Windows";
        chkStartup.AutoSize = true;
        chkStartup.Location = new Point(34, 150);
        btnSaveSettings.Text = "Сохранить настройки";
        btnSaveSettings.Location = new Point(34, 205);
        btnSaveSettings.Size = new Size(190, 38);
        btnSaveSettings.Click += btnSaveSettings_Click;
        var note = new Label { Text = "Приложение проверяет даты рождения при работе в фоне (в системном трее).\nВозрастные рубежи — напоминание проверить документ, а не подтверждение просрочки.", AutoSize = true, Location = new Point(34, 280), ForeColor = Color.DimGray };
        tabSettings.Controls.AddRange(new Control[] { settingsTitle, warningLabel, nudWarningDays, chkStartup, btnSaveSettings, note });

        trayMenu.Items.AddRange(new ToolStripItem[] { trayOpen, trayExit });
        trayOpen.Click += trayOpen_Click;
        trayExit.Click += trayExit_Click;
        trayIcon.Text = "Student Passport Monitor";
        trayIcon.Icon = SystemIcons.Application;
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.DoubleClick += trayOpen_Click;
        ResumeLayout(false);
    }

    private static Panel MakeCard(string caption, Label value, int x)
    {
        var panel = new Panel
        {
            Location = new Point(x, 100), Size = new Size(245, 100),
            BackColor = Color.FromArgb(246, 248, 251), BorderStyle = BorderStyle.FixedSingle
        };
        var title = new Label { Text = caption, Location = new Point(15, 15), AutoSize = true, ForeColor = Color.DimGray };
        value.Text = "0";
        value.Font = new Font("Segoe UI", 25F, FontStyle.Bold);
        value.Location = new Point(15, 40);
        value.AutoSize = true;
        panel.Controls.AddRange(new Control[] { title, value });
        return panel;
    }
}