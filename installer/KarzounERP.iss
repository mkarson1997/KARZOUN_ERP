#define MyAppName "KARZOUN ERP"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Karzoun"
#define MyAppURL "https://github.com/mkarson1997/KARZOUN_ERP"
#define MyAppExeName "KARZOUN_ERP.exe"

[Setup]
AppId={{A389E47C-CC27-493F-A882-B215599A18CB}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\KARZOUN ERP
DefaultGroupName=KARZOUN ERP
DisableProgramGroupPage=yes
UninstallDisplayName=KARZOUN ERP
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=output
OutputBaseFilename=KARZOUN_ERP_Setup_1.1.0
SetupIconFile=..\Resources\Brand\InstallerIcon.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
CloseApplications=yes
RestartApplications=no
UsePreviousAppDir=yes
VersionInfoVersion=1.1.0.0
VersionInfoCompany=Karzoun
VersionInfoDescription=KARZOUN ERP Setup
VersionInfoProductName=KARZOUN ERP
VersionInfoProductVersion=1.1.0

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\KARZOUN ERP"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\KARZOUN ERP"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch KARZOUN ERP"; Flags: nowait postinstall skipifsilent
