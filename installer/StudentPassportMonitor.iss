#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef AppExe
  #define AppExe "StudentPassportMonitor.exe"
#endif
#ifndef PublishDir
  #define PublishDir "..\\artifacts\\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\\artifacts\\installer"
#endif

[Setup]
AppId={{BAAE68CB-60C7-4E7D-B7C0-1DA51427F917}
AppName=Student Passport Monitor
AppVersion={#AppVersion}
AppVerName=Student Passport Monitor {#AppVersion}
DefaultDirName={autopf}\\Student Passport Monitor
DefaultGroupName=Student Passport Monitor
OutputDir={#OutputDir}
OutputBaseFilename=StudentPassportMonitor-Setup-v{#AppVersion}-win-x64
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\\{#AppExe}
WizardStyle=modern

[Files]
Source: "{#PublishDir}\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\\Student Passport Monitor"; Filename: "{app}\\{#AppExe}"
Name: "{autodesktop}\\Student Passport Monitor"; Filename: "{app}\\{#AppExe}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные задачи:"

[Run]
Filename: "{app}\\{#AppExe}"; Description: "Запустить Student Passport Monitor"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Intentionally do not delete per-user student records in LOCALAPPDATA.
