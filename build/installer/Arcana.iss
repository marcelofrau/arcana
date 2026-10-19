#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef SourceRoot
  #define SourceRoot "..\dist\stage-win-x64"
#endif
#ifndef OutputRoot
  #define OutputRoot "..\dist"
#endif
#ifndef TargetArch
  #define TargetArch "x64"
#endif

[Setup]
AppId={{E0B54516-1FBD-4BAF-A951-8CB4F84B2112}
AppName=Arcana
AppVersion={#AppVersion}
AppPublisher=Marcelo Frau
AppPublisherURL=https://github.com/marcelofrau/arcana
DefaultDirName={localappdata}\Programs\Arcana
DefaultGroupName=Arcana
PrivilegesRequired=lowest
OutputDir={#OutputRoot}
OutputBaseFilename=Arcana-v{#AppVersion}-win-{#TargetArch}-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\Arcana.App\Arcana.App.exe
ChangesAssociations=yes
#if TargetArch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
LicenseFile={#SourceRoot}\LICENSE

[Tasks]
Name: "contextmenu"; Description: "Add Arcana to the Explorer context menu"
Name: "associations"; Description: "Offer Arcana in Open with for ZIP, 7z, RAR and TAR"; Flags: unchecked
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "{#SourceRoot}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Arcana"; Filename: "{app}\Arcana.App\Arcana.App.exe"
Name: "{autodesktop}\Arcana"; Filename: "{app}\Arcana.App\Arcana.App.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Classes\Arcana.Archive"; ValueType: string; ValueData: "Arcana archive"; Flags: uninsdeletekey; Tasks: associations
Root: HKCU; Subkey: "Software\Classes\Arcana.Archive\DefaultIcon"; ValueType: string; ValueData: """{app}\Arcana.App\Arcana.App.exe"",0"; Tasks: associations
Root: HKCU; Subkey: "Software\Classes\Arcana.Archive\shell\open\command"; ValueType: string; ValueData: """{app}\Arcana.App\Arcana.App.exe"" ""%1"""; Tasks: associations
Root: HKCU; Subkey: "Software\Classes\.zip\OpenWithProgids"; ValueType: string; ValueName: "Arcana.Archive"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associations
Root: HKCU; Subkey: "Software\Classes\.7z\OpenWithProgids"; ValueType: string; ValueName: "Arcana.Archive"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associations
Root: HKCU; Subkey: "Software\Classes\.rar\OpenWithProgids"; ValueType: string; ValueName: "Arcana.Archive"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associations
Root: HKCU; Subkey: "Software\Classes\.tar\OpenWithProgids"; ValueType: string; ValueName: "Arcana.Archive"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associations
Root: HKCU; Subkey: "Software\Classes\*\shell\Arcana.Open"; ValueType: string; ValueData: "Open with Arcana"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\*\shell\Arcana.Open\command"; ValueType: string; ValueData: """{app}\Arcana.App\Arcana.App.exe"" ""%1"""; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\*\shell\Arcana.Extract"; ValueType: string; ValueData: "Extract with Arcana..."; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\*\shell\Arcana.Extract\command"; ValueType: string; ValueData: """{app}\Arcana.App\Arcana.App.exe"" --extract ""%1"""; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Arcana.Create"; ValueType: string; ValueData: "Compress with Arcana..."; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCU; Subkey: "Software\Classes\Directory\shell\Arcana.Create\command"; ValueType: string; ValueData: """{app}\Arcana.App\Arcana.App.exe"" --create ""%1"""; Tasks: contextmenu

[Run]
Filename: "{app}\Arcana.App\Arcana.App.exe"; Description: "Launch Arcana"; Flags: nowait postinstall skipifsilent
