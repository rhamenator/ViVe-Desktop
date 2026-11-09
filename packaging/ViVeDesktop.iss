#ifndef AppVersion
  #define AppVersion "1.8.0"
#endif
#ifndef PayloadDir
  #define PayloadDir "..\artifacts\desktop"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\release"
#endif
[Setup]
AppId={{1D7797CA-D91B-44C0-A98D-700859DBA62A}
AppName=ViVe Desktop
AppVersion={#AppVersion}
AppPublisher=Richard M. Hamilton
AppPublisherURL=https://github.com/rhamenator/ViVe-Desktop
AppSupportURL=https://github.com/rhamenator/ViVe-Desktop/issues
AppUpdatesURL=https://github.com/rhamenator/ViVe-Desktop/releases
DefaultDirName={localappdata}\Programs\ViVeDesktop
DefaultGroupName=ViVe Desktop
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
LicenseFile=..\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=ViVe-Desktop-{#AppVersion}-Setup-x64
SetupIconFile=..\ViVe.Desktop\Assets\App.ico
UninstallDisplayIcon={app}\ViVe.Desktop.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"
[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\ViVe Desktop"; Filename: "{app}\ViVe.Desktop.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\ViVe Desktop"; Filename: "{app}\ViVe.Desktop.exe"; WorkingDir: "{app}"; Tasks: desktopicon
[Run]
Filename: "{app}\ViVe.Desktop.exe"; Description: "Open ViVe Desktop (administrator access required)"; Flags: nowait postinstall skipifsilent runasoriginaluser
