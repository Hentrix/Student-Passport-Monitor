using ClosedXML.Excel;
using StudentPassportMonitor.Core;
using StudentPassportMonitor.Services;

namespace StudentPassportMonitor.Services;

public enum ExcelReportType
{
    AllStudents,
    Attention,
    ByGroup,
    MissingData
}

public sealed class ExcelReportService
{
    public int Export(string path, ExcelReportType type, IReadOnlyList<StudentRow> allRows,
        string? group, DateTime generatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(allRows);

        IEnumerable<StudentRow> query = type switch
        {
            ExcelReportType.Attention => allRows.Where(r =>
                r.AlertStatus is PassportAlertState.Upcoming or
                    PassportAlertState.ConfirmationRequired or
                    PassportAlertState.ReviewRequired),
            ExcelReportType.ByGroup => allRows.Where(r =>
                string.Equals(r.Group, group, StringComparison.CurrentCultureIgnoreCase)),
            ExcelReportType.MissingData => allRows.Where(r =>
                r.Student.BirthDate is null ||
                string.IsNullOrWhiteSpace(r.Student.PassportNumber) ||
                r.Student.IssueDate is null),
            _ => allRows
        };
        StudentRow[] rows = query.OrderBy(r => r.Group).ThenBy(r => r.FullName).ToArray();

        using var book = new XLWorkbook();
        var sheet = book.Worksheets.Add("Отчёт");
        string title = type switch
        {
            ExcelReportType.Attention => "Студенты, требующие внимания",
            ExcelReportType.ByGroup => $"Студенты группы {group}",
            ExcelReportType.MissingData => "Студенты с неполными сведениями",
            _ => "Реестр студентов"
        };
        sheet.Cell(1, 1).Value = title;
        sheet.Range(1, 1, 1, 9).Merge();
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;
        sheet.Cell(2, 1).Value = $"Сформирован: {generatedAt:dd.MM.yyyy HH:mm}; записей: {rows.Length}";
        sheet.Range(2, 1, 2, 9).Merge();

        string[] headers =
        [
            "№", "ФИО", "Группа", "Факультет", "Дата рождения",
            "Возраст", "Контрольная дата", "Состояние", "Последняя проверка"
        ];
        for (int i = 0; i < headers.Length; i++)
            sheet.Cell(4, i + 1).Value = headers[i];

        var header = sheet.Range(4, 1, 4, 9);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#253858");
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Alignment.WrapText = true;

        for (int i = 0; i < rows.Length; i++)
        {
            StudentRow r = rows[i];
            int row = i + 5;
            sheet.Cell(row, 1).Value = i + 1;
            sheet.Cell(row, 2).Value = SafeText(r.FullName);
            sheet.Cell(row, 3).Value = SafeText(r.Group);
            sheet.Cell(row, 4).Value = SafeText(r.Faculty);
            SetDate(sheet.Cell(row, 5), r.Student.BirthDate);
            sheet.Cell(row, 6).Value = r.Age == "—" ? "" : r.Age;
            if (DateTime.TryParseExact(r.DueDate, "dd.MM.yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var due))
                SetDate(sheet.Cell(row, 7), due);
            sheet.Cell(row, 8).Value = SafeText(r.State);
            SetDate(sheet.Cell(row, 9), r.Student.LastReviewedAt, true);

            if (r.AlertStatus == PassportAlertState.ReviewRequired)
                sheet.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#FCE8E6");
            else if (r.AlertStatus is PassportAlertState.Upcoming or PassportAlertState.ConfirmationRequired)
                sheet.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2D6");
            else if (i % 2 == 1)
                sheet.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#F4F7FA");
        }

        sheet.Columns(1, 9).AdjustToContents(4, Math.Max(4, rows.Length + 4), 8, 42);
        sheet.Column(2).Width = Math.Max(sheet.Column(2).Width, 28);
        sheet.Column(8).Width = Math.Max(sheet.Column(8).Width, 28);
        sheet.SheetView.FreezeRows(4);
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.SetRowsToRepeatAtTop(1, 4);
        sheet.Range(4, 1, Math.Max(4, rows.Length + 4), 9).SetAutoFilter();

        // Save only after the workbook has been constructed successfully.
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, $".{Guid.NewGuid():N}.xlsx");
        try
        {
            book.SaveAs(temporary);
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }

        return rows.Length;
    }

    private static void SetDate(IXLCell cell, DateTime? value, bool withTime = false)
    {
        if (value is not { } date) return;
        cell.Value = date;
        cell.Style.DateFormat.Format = withTime ? "dd.mm.yyyy hh:mm" : "dd.mm.yyyy";
    }

    // Avoid spreadsheet formula injection from imported names and other text.
    private static string SafeText(string? value)
    {
        string text = value ?? "";
        return text.Length > 0 && "=+-@".Contains(text[0]) ? "'" + text : text;
    }
}
