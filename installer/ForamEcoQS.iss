#ifndef AppVersion
  #define AppVersion "1.1.4"
#endif

#ifndef PublishDir
  #error PublishDir must point to the self-contained dotnet publish directory.
#endif

#ifndef OutputDir
  #define OutputDir "."
#endif

#define AppName "ForamEcoQS"
#define AppExeName "ForamEcoQS.exe"

[Setup]
AppId={{8D4D917F-83A5-4E06-9CA8-81421538162D}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=University of Urbino Carlo Bo
AppPublisherURL=https://github.com/uniurbit/ForamEcoQS
AppSupportURL=https://github.com/uniurbit/ForamEcoQS/issues
AppUpdatesURL=https://github.com/uniurbit/ForamEcoQS/releases
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=ForamEcoQS-v{#AppVersion}-win-x64-setup
SetupIconFile=..\ForamEcoQS\favicon.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
ChangesAssociations=no
VersionInfoVersion={#AppVersion}.0
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
VersionInfoDescription={#AppName} Windows x64 installer
VersionInfoCompany=University of Urbino Carlo Bo

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
