; ARSVIN Suite installer
; Built by .github/workflows/release.yml with Inno Setup 6.

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif

#ifndef SourceDir
  #define SourceDir "..\artifacts\installer-input"
#endif

#ifndef OutputDir
  #define OutputDir "..\artifacts\release"
#endif

#define MyAppName "ARSVIN Suite"
#define MyAppPublisher "Ari Sulistiono"
#define MyAppUrl "https://github.com/masarray/arsvin"

[Setup]
AppId={{A76AC909-93A0-4EA3-A6EE-DA3B64595F52}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppUrl}
AppSupportURL={#MyAppUrl}/issues
AppUpdatesURL={#MyAppUrl}/releases
DefaultDirName={localappdata}\Programs\ARSVIN
DefaultGroupName=ARSVIN
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=ARSVIN-Suite-Setup-win-x64
SetupIconFile=..\src\ARSVIN\Assets\arsvin.ico
UninstallDisplayIcon={app}\ARSVIN.exe
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
MinVersion=10.0
CloseApplications=yes
RestartApplications=no
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=ARSVIN IEC 61850 Sampled Values engineering suite
VersionInfoProductName={#MyAppName}
LicenseFile=..\LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut for ARSVIN Publisher"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\ARSVIN.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\ArSubsv.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\NOTICE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\COMMERCIAL-LICENSE.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\COPYRIGHT.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\TRADEMARK.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\VERSION.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourceDir}\samples\*"; DestDir: "{app}\samples"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\ARSVIN Publisher"; Filename: "{app}\ARSVIN.exe"; WorkingDir: "{app}"
Name: "{group}\ArSubsv Subscriber"; Filename: "{app}\ArSubsv.exe"; WorkingDir: "{app}"
Name: "{group}\Documentation"; Filename: "{app}\README.md"
Name: "{group}\GPL License"; Filename: "{app}\LICENSE.txt"
Name: "{group}\Commercial Licensing Notice"; Filename: "{app}\COMMERCIAL-LICENSE.md"
Name: "{group}\Project website"; Filename: "https://masarray.github.io/arsvin/"
Name: "{group}\Uninstall ARSVIN Suite"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ARSVIN Publisher"; Filename: "{app}\ARSVIN.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\ARSVIN.exe"; Description: "Launch ARSVIN Publisher"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[Code]
function NpcapInstalled(): Boolean;
begin
  Result :=
    FileExists(ExpandConstant('{sys}\Npcap\wpcap.dll')) or
    FileExists(ExpandConstant('{sys}\wpcap.dll'));
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and (not WizardSilent) and (not NpcapInstalled()) then
  begin
    SuppressibleMsgBox(
      'ARSVIN was installed successfully.' + #13#10 + #13#10 +
      'Live IEC 61850 Sampled Values capture and transmission require Npcap. ' +
      'Install Npcap separately from its official website before using authorized live network features.',
      mbInformation,
      MB_OK,
      IDOK
    );
  end;
end;