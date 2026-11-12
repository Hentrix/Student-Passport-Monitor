using System.Windows;
using StudentPassportMonitor.Models;

namespace StudentPassportMonitor.Wpf;

public partial class StudentEditorWindow : Window
{
    private readonly Student? _original;
    public Student? Result { get; private set; }

    public StudentEditorWindow(Student? original)
    {
        InitializeComponent();
        _original = original;
        if (original is null) return;
        LastNameBox.Text = original.LastName;
        FirstNameBox.Text = original.FirstName;
        MiddleNameBox.Text = original.MiddleName;
        GroupBox.Text = original.Group;
        FacultyBox.Text = original.Faculty;
        BirthDatePicker.SelectedDate = original.BirthDate;
        StatusBox.Text = original.Status;
        PassportNumberBox.Text = original.PassportNumber;
        IssuedByBox.Text = original.IssuedBy;
        IssueDatePicker.SelectedDate = original.IssueDate;
        ReplacementDatePicker.SelectedDate = original.LastPassportReplacementDate;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(LastNameBox.Text) || string.IsNullOrWhiteSpace(FirstNameBox.Text))
        {
            MessageBox.Show(this, "Укажите фамилию и имя."); return;
        }

        Result = new Student
        {
            Id = _original?.Id ?? Guid.NewGuid(),
            LastName = LastNameBox.Text.Trim(),
            FirstName = FirstNameBox.Text.Trim(),
            MiddleName = MiddleNameBox.Text.Trim(),
            Group = GroupBox.Text.Trim(),
            Faculty = FacultyBox.Text.Trim(),
            BirthDate = BirthDatePicker.SelectedDate,
            Status = StatusBox.Text.Trim(),
            PassportNumber = PassportNumberBox.Text.Trim(),
            IssuedBy = IssuedByBox.Text.Trim(),
            IssueDate = IssueDatePicker.SelectedDate,
            LastPassportReplacementDate = ReplacementDatePicker.SelectedDate,
            LastReviewedAt = _original?.LastReviewedAt
        };
        DialogResult = true;
    }
}
