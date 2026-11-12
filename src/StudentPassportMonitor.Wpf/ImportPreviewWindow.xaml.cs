using System.Windows;
using StudentPassportMonitor.Core;

namespace StudentPassportMonitor.Wpf;

public partial class ImportPreviewWindow : Window
{
    public bool ReplaceExisting { get; private set; }

    public ImportPreviewWindow(StudentMonitorController.ImportPreview preview, string fileName)
    {
        InitializeComponent();
        FileNameText.Text = fileName;
        SummaryText.Text = $"Строк: {preview.Students.Count}  •  Новых: {preview.NewStudents}  •  Совпадений с базой: {preview.ExistingMatches}";
        WarningText.Text = $"Повторы в файле: {preview.DuplicateRows}  •  Без даты рождения: {preview.MissingBirthDates}  •  Без номера паспорта: {preview.MissingPassportNumbers}";
        PreviewGrid.ItemsSource = preview.Students;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Merge_Click(object sender, RoutedEventArgs e)
    {
        ReplaceExisting = false;
        DialogResult = true;
    }

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this,
                "Полная замена удалит студентов, отсутствующих в Excel, и может стереть вручную внесённые сведения. Продолжить?",
                "Подтверждение полной замены", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        ReplaceExisting = true;
        DialogResult = true;
    }
}
