; Script de Inno Setup para Dynamic Island for Windows
; Requiere Inno Setup 6 o superior

#define MyAppName "Dynamic Island for Windows"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "VonRabbid"
#define MyAppURL "https://github.com/VonRabbid/Dynamic-Island"
#define MyAppExeName "DynamicIsland.exe"

[Setup]
AppId={{5D46B6B5-6490-449D-9457-3F79E89D5180}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\Dynamic Island
DefaultGroupName=Dynamic Island
DisableProgramGroupPage=yes
OutputDir=..\publish_installer
OutputBaseFilename=DynamicIsland-Setup-v{#MyAppVersion}
SetupIconFile=..\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/normal
SolidCompression=no
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline

; Metadatos para evitar falsos positivos heurísticos
VersionInfoVersion=1.0.0.0
VersionInfoCompany=VonRabbid
VersionInfoDescription=Instalador oficial de Dynamic Island para Windows
VersionInfoCopyright=Copyright © 2026 VonRabbid
VersionInfoProductName=Dynamic Island
VersionInfoProductVersion=1.0.0.0

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Iniciar Dynamic Island automáticamente al encender Windows"; GroupDescription: "Inicio de Windows:"; Flags: checkedonce

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "DynamicIsland"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Ejecutar Dynamic Island ahora"; Flags: nowait postinstall skipifsilent
