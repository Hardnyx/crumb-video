#ifndef PackageDir
  #error Pass /DPackageDir with the published portable directory.
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts"
#endif
[Setup]
AppId={{7E803AB1-2BFE-47BD-97F8-5B7E6588FEF0}
AppName=Crumb
AppVersion=0.1.0
AppPublisher=Hardnyx
AppPublisherURL=https://github.com/Hardnyx/crumb-video
DefaultDirName={localappdata}\Programs\Crumb
DefaultGroupName=Crumb
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=Crumb-0.1.0-setup-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\Crumb.exe
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
[Files]
Source: "{#PackageDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Crumb"; Filename: "{app}\Crumb.exe"
Name: "{autodesktop}\Crumb"; Filename: "{app}\Crumb.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\Crumb.exe"; Description: "Launch Crumb"; Flags: nowait postinstall skipifsilent
