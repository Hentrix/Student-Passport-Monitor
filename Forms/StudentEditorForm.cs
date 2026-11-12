using System.Globalization;
using StudentPassportMonitor.Models;

namespace StudentPassportMonitor.Forms;

// Self-contained editor; no persistence or business rules in the UI.
public sealed class StudentEditorForm : Form
{
    private readonly Dictionary<string, TextBox> _fields = new();
    private readonly Student _original;
    private readonly ComboBox _statusBox = new();

    public Student? Result { get; private set; }

    public StudentEditorForm(Student? student = null)
    {
        _original = student ?? new Student();
        Text = student is null ? "Добавить студента" : "Карточка студента";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(520, 650);
        Size = new Size(620, 720);
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Font;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoScroll = true,
            Padding = new Padding(18)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddField(layout, "Фамилия *", "LastName", _original.LastName);
        AddField(layout, "Имя *", "FirstName", _original.FirstName);
        AddField(layout, "Отчество", "MiddleName", _original.MiddleName);
        AddField(layout, "Группа", "Group", _original.Group);
        AddField(layout, "Факультет", "Faculty", _original.Faculty);
        AddField(layout, "Дата рождения", "BirthDate", Format(_original.BirthDate));
        AddField(layout, "Номер паспорта", "PassportNumber", _original.PassportNumber);
        AddField(layout, "Кем выдан", "IssuedBy", _original.IssuedBy);
        AddField(layout, "Дата выдачи", "IssueDate", Format(_original.IssueDate));
        AddField(layout, "Дата замены", "ReplacementDate", Format(_original.LastPassportReplacementDate));
        AddStatusField(layout, _original.Status);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 65,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12)
        };
        var save = new Button { Text = "Сохранить", Width = 125, Height = 34 };
        var cancel = new Button { Text = "Отмена", Width = 110, Height = 34, DialogResult = DialogResult.Cancel };
        save.Click += Save_Click;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        Controls.Add(layout);
        Controls.Add(buttons);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void AddField(TableLayoutPanel layout, string label, string key, string value)
    {
        int row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 47));
        layout.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, row);

        var field = new TextBox
        {
            Text = value,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 5, 4, 8)
        };
        if (key.Contains("Date", StringComparison.Ordinal))
            field.PlaceholderText = "ДД.ММ.ГГГГ";
        _fields[key] = field;
        layout.Controls.Add(field, 1, row);
    }

    private void AddStatusField(TableLayoutPanel layout, string current)
    {
        int row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 47));
        layout.Controls.Add(new Label
        {
            Text = "Статус",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, row);

        _statusBox.Name = "cmbStudentStatus";
        _statusBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusBox.Dock = DockStyle.Fill;
        _statusBox.Margin = new Padding(4, 5, 4, 8);
        _statusBox.Items.AddRange(new object[]
        {
            "Учащийся", "Академический отпуск", "Отчислен", "Выпускник"
        });
        // Preserve values from previously imported spreadsheets.
        if (!string.IsNullOrWhiteSpace(current) && !_statusBox.Items.Contains(current))
            _statusBox.Items.Add(current);
        _statusBox.SelectedItem = string.IsNullOrWhiteSpace(current)
            ? "Учащийся" : current;
        layout.Controls.Add(_statusBox, 1, row);
    }

    private static string Format(DateTime? value) => value?.ToString("dd.MM.yyyy") ?? "";

    private DateTime? ReadDate(string key)
    {
        string value = _fields[key].Text.Trim();
        if (value.Length == 0) return null;
        if (!DateTime.TryParseExact(value, "dd.MM.yyyy",
                CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.None, out var date))
            throw new ArgumentException("Неверная дата. Используйте ДД.ММ.ГГГГ.");
        return date.Date;
    }

    private void Save_Click(object? sender, EventArgs e)
    {
        try
        {
            string Get(string key) => _fields[key].Text.Trim();
            Result = new Student
            {
                Id = _original.Id,
                LastReviewedAt = _original.LastReviewedAt,
                LastName = Get("LastName"),
                FirstName = Get("FirstName"),
                MiddleName = Get("MiddleName"),
                Group = Get("Group"),
                Faculty = Get("Faculty"),
                BirthDate = ReadDate("BirthDate"),
                PassportNumber = Get("PassportNumber"),
                IssuedBy = Get("IssuedBy"),
                IssueDate = ReadDate("IssueDate"),
                LastPassportReplacementDate = ReadDate("ReplacementDate"),
                Status = _statusBox.SelectedItem?.ToString() ?? ""
            };
            if (string.IsNullOrWhiteSpace(Result.LastName) ||
                string.IsNullOrWhiteSpace(Result.FirstName))
                throw new ArgumentException("Фамилия и имя обязательны.");

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "Проверьте данные",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
