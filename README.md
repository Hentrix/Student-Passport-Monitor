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

## Windows notifications

This version uses the existing WinForms tray icon to show Windows balloon notifications. It does not yet implement modern Windows toast notifications. Native toast integration is planned as a separate improvement.

## Data files

- `students.json` — imported student records.
- `settings.json` — warning period, startup option and last check date.

The app never uploads student data to GitHub or any remote service.


## Архитектура

- `Forms/MainForm.Designer.cs` — только компоненты и их расположение; редактируется в Visual Studio Designer.
- `Forms/MainForm.cs` — только отображение, события, диалоги, системный трей и таймер.
- `Core/StudentMonitorController.cs` — сценарии приложения: импорт, фильтрация, статистика, сохранение, ежедневная проверка.
- `Models/` — данные студентов и настройки.
- `Services/` — чтение Excel, хранение JSON, правила возрастных рубежей, объединение списков, автозапуск.

Важное ограничение: контроллер не зависит от WinForms, но пока непосредственно создаётся формой и использует конкретные сервисы. Для независимых модульных тестов следующим шагом можно ввести интерфейсы репозитория, часов и импорта.

### Сборка

Перед `dotnet build` полностью выйдите из приложения через значок в трее: закрытие окна лишь скрывает его. Если исполняемый файл заблокирован, используйте `Stop-Process -Name StudentPassportMonitor -ErrorAction SilentlyContinue`.

```powershell
dotnet restore
dotnet build
```

**Данные:** список студентов и паспортные сведения хранятся локально в `%LOCALAPPDATA%\StudentPassportMonitor`. Не добавляйте их в Git. На рабочих компьютерах используйте защиту диска и ограничение доступа к профилю пользователя.

**Примечание:** возрастные рубежи 20 и 45 лет — повод проверить документ, а не юридическое подтверждение просрочки. Уведомления реализованы через значок в трее, не через Windows Toast.


## Этап 2: контроль паспортных рубежей

Контрольные возраста — 20 и 45 лет. Для каждого рубежа приложение проверяет, подтверждается ли замена датой выдачи текущего паспорта (`IssueDate`) или отдельно внесённой датой замены (`LastPassportReplacementDate`), не более ранней, чем соответствующий день рождения.

- До настроенного порога предупреждения: «Запланировано».
- В пределах порога до дня рождения: «Скоро 20/45 лет», оранжевый.
- От даты рождения до 90 дней после неё включительно: «Рубеж наступил — проверьте замену», оранжевый.
- Позднее 90 дней без подтверждённой замены: «Требуется проверка паспорта», красный.
- Если замена подтверждена, приложение переходит к следующему рубежу или показывает «Замена подтверждена».

**Важно:** 90 дней здесь — интервал приоритизации внутренней проверки, а не автоматическое юридическое заключение о недействительности документа. Сроки, исключения и действительность паспорта необходимо сверять с действующим законодательством РФ. Если дата выдачи в исходной ведомости неактуальна, исправьте её в карточке студента. Отсутствие даты рождения не позволяет рассчитать рубеж.


## Этап 3: безопасный импорт и резервные копии

При выборе Excel приложение сначала показывает количество найденных студентов, количество отсутствующих дат рождения и номеров паспортов, а также три примера строк. Подтверждение предлагает **объединение** (рекомендуется) или **полную замену**. Перед каждым подтверждённым импортом создаётся резервная копия текущего `students.json`.

Копии сохраняются в `%LOCALAPPDATA%\\StudentPassportMonitor\\backups`. В настройках доступна кнопка **«Восстановить базу»**: перед восстановлением также создаётся копия текущей базы. При объединении пустые импортированные ячейки не затирают существующие паспортные данные, дату рождения, статус и факультет; дата замены сохраняется всегда.

**Ограничения:** совпадение студентов пока определяется по ФИО и группе. Изменение группы или фамилии может создать дубликат. При явном выборе полной замены существующие ручные правки могут исчезнуть; восстановить их можно из копии. Копии содержат персональные данные без дополнительного шифрования — ограничьте доступ к профилю Windows и включите шифрование диска. Отчёт о каждой пропущенной строке и ручное сопоставление столбцов пока не реализованы.


## Этап 4: центр уведомлений

Вкладка **«Уведомления»** показывает студентов с приближающимися или наступившими контрольными датами, текущий статус, дату рубежа и дату последней ручной проверки. Двойной щелчок или кнопка **«Открыть карточку»** открывает редактор. Кнопка **«Отметить проверенным»** сохраняет только факт проверки сведений; она **не** подтверждает замену паспорта и **не** скрывает предупреждение. Подтвердить замену можно через дату выдачи или замены паспорта.

История ручных и ежедневных автоматических проверок (время и число требующих внимания) хранится локально в `%LOCALAPPDATA%\\StudentPassportMonitor\\check-history.json`, отображаются последние 100 записей, хранятся до 200. Существующие уведомления в трее продолжают работать. История не содержит персональных данных студентов, но журнал ручных отметок пока хранит только последнюю дату на каждого студента.


## Этап 5: экспорт Excel

На вкладке «Студенты» кнопка **«Экспорт Excel»** открывает выбор отчёта: все студенты, требующие внимания, по группе или с неполными сведениями. Затем пользователь выбирает место сохранения файла `.xlsx`.

В отчёте есть ФИО, группа, факультет, дата рождения, возраст, контрольная дата, состояние и дата последней проверки. Номер паспорта намеренно не экспортируется. Форматирование включает фильтры, закреплённую шапку, цветовую маркировку, альбомную печать и повторение заголовков при печати. Используется библиотека ClosedXML.

## Этап 6: версии, тесты и Windows-релизы

Версия задаётся в `Directory.Build.props` через `VersionPrefix` в формате `MAJOR.MINOR.PATCH`. Начальная версия — **1.0.0**. MAJOR увеличивается при несовместимых изменениях, MINOR — при добавлении совместимых функций, PATCH — при исправлениях. Версия отображается в заголовке приложения и попадает в свойства EXE.

Запуск тестов:

```powershell
dotnet test tests/StudentPassportMonitor.Tests/StudentPassportMonitor.Tests.csproj -c Release
```

Тесты проверяют расчёт возрастных рубежей, подтверждение замены и сохранность сведений при объединении. Тестовый проект `net10.0` не зависит от WinForms и может запускаться на Linux. CI запускает тесты при push в `main` и при pull request. Windows-сборка выполняется только после успешных тестов.

Для публикации новой версии:
1. Измените `VersionPrefix` в `Directory.Build.props` (например, на `1.1.0`), закоммитьте и отправьте изменения.
2. Убедитесь, что GitHub Actions для ветки `main` завершился успешно.
3. Создайте и отправьте тег **точно соответствующей версии**:

```powershell
git tag v1.1.0
git push origin v1.1.0
```

Workflow `.github/workflows/build-release.yml` проверит тег, запустит тесты, соберёт Windows x64 self-contained приложение, установщик Inno Setup и файл `SHA256SUMS.txt`. После успешной сборки создаст GitHub Release с установщиком и автоматически сформированными заметками. На обычных push в `main` установщик доступен как artifact запуска Actions, но GitHub Release не создаётся.

**Важно:** выпуск версии требует доступного GitHub Actions и разрешения на создание Releases. Установщик не подписан сертификатом кода; Windows SmartScreen может показывать предупреждение. Персональные данные из локального профиля пользователя в установщик не включаются. При удалении программы файлы данных в `%LOCALAPPDATA%` намеренно сохраняются.

