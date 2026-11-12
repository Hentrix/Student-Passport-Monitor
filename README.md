# Student Passport Monitor

Windows desktop app for importing a student roster from Excel, storing it locally as JSON and monitoring age-based passport checkpoints (20 and 45 years).

## Requirements

- Windows 10/11
- .NET 10 SDK
- Visual Studio 2026 with the **.NET desktop development** workload (or a recent Visual Studio that supports .NET 10)

## Run

1. Clone this private repository.
2. Open `StudentPassportMonitor.csproj` in Visual Studio.
3. Restore NuGet packages and press F5.

## Features

- Import legacy `.xls` and modern `.xlsx` workbooks.
- Finds columns by Russian/English header names; the first worksheet containing Фамилия and Имя is imported.
- Local JSON data under `%LOCALAPPDATA%\StudentPassportMonitor\`.
- Configurable warning window, default 90 days.
- Daily check while the app is running (including in the tray).
- Closing the window hides it to the notification area; tray menu can reopen or exit.
- Optional per-user Windows startup entry.

## Important data and legal notes

The age checkpoints are reminders to verify a document, not proof that a passport is expired or that a replacement is legally overdue. A student may already have replaced a passport. Enter the actual last replacement date in the student record when known. Verify current legal requirements separately.

Student records may contain passport numbers and dates of birth. They are **not** included in this repository. Keep the local data directory restricted to the Windows account that needs access, enable BitLocker, and do not commit real student data. JSON files are plaintext; this initial version does not encrypt them.

## Toast notifications

Windows toast delivery for unpackaged WinForms apps can require a Start Menu shortcut and a stable AppUserModelID. If notifications do not appear on a particular machine, the dashboard and tray reminders remain available; configure the shortcut/AUMID for that deployment.

## Data files

- `students.json` — imported student records.
- `settings.json` — warning period, startup option and last check date.

The app never uploads student data to GitHub or any remote service.
