; Windows installer for Nudge (Inno Setup 6).
; Build: publish the app first, then compile this script with ISCC.exe.

#define AppName "Nudge"
#define AppExe "Nudge.exe"
#define SourceExe AddBackslash(SourcePath) + "..\artifacts\publish\" + AppExe

#if !FileExists(SourceExe)
  #error Nudge.exe not found. Run first: dotnet publish src/Nudge.App -p:PublishProfile=win-x64
#endif

; Version comes from the published exe, so Directory.Build.props stays the single source of truth.
#define AppVersion GetVersionNumbersString(SourceExe)

[Setup]
AppId={{8E4B7F2A-3C1D-4E5F-9A6B-7C8D9E0F1A2B}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=Chani Pappenheim
; Per-user install: no administrator rights needed, and data and startup settings are per-user anyway.
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=..\artifacts\installer
OutputBaseFilename=NudgeSetup-{#AppVersion}
SetupIconFile=..\src\Nudge.App\Assets\nudge.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
; Nudge lives in the tray, so an upgrade must close the running copy first.
CloseApplications=force
RestartApplications=no

[Languages]
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"

[CustomMessages]
hebrew.StartWithWindows=הפעלה אוטומטית עם הפעלת Windows
hebrew.DeleteUserData=למחוק גם את התזכורות וההיסטוריה השמורות?%n%nאם תבחר "לא", הן יישמרו ויחזרו אם תתקין את Nudge שוב.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "{cm:StartWithWindows}"; Flags: unchecked

[Files]
Source: "{#SourceExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Same value the app's own "start with Windows" checkbox writes, so the two stay in sync.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExe}"" --minimized"; Tasks: startup
; Removed on uninstall even when it was turned on from inside the app.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "{#AppName}"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stops this user's running copy so its files can be removed.
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM {#AppExe} /FI ""USERNAME eq {username}"""; Flags: runhidden; RunOnceId: "StopNudge"

[Code]
// Reminders are user data: keep them unless the user explicitly asks to delete them.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep <> usPostUninstall then
    Exit;

  DataDir := ExpandConstant('{userappdata}\{#AppName}');
  if DirExists(DataDir) and not UninstallSilent and
     (MsgBox(CustomMessage('DeleteUserData'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES) then
    DelTree(DataDir, True, True, True);
end;
