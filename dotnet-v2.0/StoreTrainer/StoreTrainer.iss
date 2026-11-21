; ================================================================================
; FILE: StoreTrainer.iss
; APPLICATION: RaggieSoft StoreTrainer 2.0
; COMPILER: Inno Setup 7.1.1 (Native 64-Bit ISCC.exe at C:\Program Files\Inno Setup 7)
; ARCHITECTURES: Native AMD64 (win-x64) & Native ARM64 (win-arm64)
; LICENSE: MIT License (Application Code) / CC BY-SA 4.0 (Documentation & Artwork)
;
; PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
;   What is this file?
;     This '.iss' file is the Inno Setup 7 installer script that compiles
;     'RaggieSoft-StoreTrainer-Setup.exe' inside '..\Installers\'.
;
;   How does the Surgical NSIS-to-Inno Setup Upgrade (v1.0 / v1.1 -> v2.0) work?
;     Like Microsoft Office 97 (ACME Setup) upgrading cleanly to Office 2000 (MSI),
;     classic StoreTrainer v1.0 & v1.1 used a 32-bit Nullsoft NSIS installer
;     ('TSSetup.nsi') that installed 'TStore.exe' into:
;       C:\Program Files (x86)\RaggieSoft\StoreTrainer
;     and registered 32-bit Visual Basic 6.0 runtime files in 'C:\Windows\SysWOW64\'.
;
;     When a user upgrades from StoreTrainer v1.0 or v1.1 to StoreTrainer 2.0,
;     this script performs a zero-race-condition SURGICAL TAKEOVER during 'ssInstall':
;       1. NEVER touches 'C:\Windows\SysWOW64\' ('msvbvm60.dll', 'comdlg32.ocx',
;          'comctl32.ocx', 'comct232.ocx', 'wshom.ocx') so other classic VB6 apps
;          on the user's PC continue working forever!
;       2. NEVER touches the user's saved game profiles ('Documents\My StoreFiles\*.stf')
;          or settings ('%APPDATA%\RaggieSoft\StoreTrainer\TStore.ini') during an upgrade!
;       3. Reads the exact legacy 32-bit NSIS installation path from
;          'HKLM32\Software\RaggieSoft\StoreTrainer' ('Installation Folder'), deletes
;          ONLY StoreTrainer's own legacy files ('TStore.exe', 'TStore.exe.manifest',
;          'README.txt', 'LICENSE.rtf', 'TStore.ini', 'uninstall.exe', and 'Source\'),
;          removes the old folder ONLY if empty, removes '{commonprograms}\StoreTrainer.lnk',
;          and deletes the old 32-bit NSIS Uninstall key so Windows "Installed Apps"
;          shows ONE clean entry owned 100% by Inno Setup's 'unins000.exe'!
; ================================================================================

#ifndef BuildDirX64
  #define BuildDirX64 "bin\Release\net10.0-windows\win-x64\publish"
#endif
#ifndef BuildDirArm64
  #define BuildDirArm64 "bin\Release\net10.0-windows\win-arm64\publish"
#endif

; Core application identity constants
#define MyAppName "RaggieSoft StoreTrainer"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "RaggieSoft (Michael P. Ragsdale)"
#define MyAppURL "https://storetrainer.raggiesoft.com"
#define MyAppExeName "StoreTrainer.exe"
; Shared Mutex name used by BOTH VB6 v1.1 ('TStore.exe') and C# v2.0 ('StoreTrainer.exe')
#define MyAppMutex "RaggieSoft_StoreTrainer_Mutex"

; Official Microsoft .NET 10.0 Windows Desktop Runtime bootstrapper URLs (downloaded ONLY if missing)
#define DotNet10DesktopX64Url "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe"
#define DotNet10DesktopX64File "windowsdesktop-runtime-10.0-win-x64.exe"
#define DotNet10DesktopArm64Url "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-arm64.exe"
#define DotNet10DesktopArm64File "windowsdesktop-runtime-10.0-win-arm64.exe"

; --------------------------------------------------------------------------------
; [Setup] SECTION: Global installer metadata, 64-bit mode & RaggieSoft Launcher ID
; --------------------------------------------------------------------------------
[Setup]
; Matches RaggieSoftSuiteManager.StoreTrainerInnoAppId in RaggieSoft.Core
AppId={{8E92D4A1-63F0-4C8B-9B7A-10164500B004}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppCopyright=Copyright (C) Michael P. Ragsdale (Code: MIT / Content: CC BY-SA 4.0)
AppMutex={#MyAppMutex}
CloseApplications=yes
DefaultDirName={autopf}\RaggieSoft\StoreTrainer
DefaultGroupName=RaggieSoft\{#MyAppName}
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64os arm64
ArchitecturesInstallIn64BitMode=x64os arm64
ChangesAssociations=yes
LicenseFile=Assets\License-StoreTrainer.rtf
OutputDir=..\Installers
OutputBaseFilename=RaggieSoft-StoreTrainer-Setup
SetupIconFile=Assets\StoreTrainer.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
VersionInfoVersion=2.0.0.0
VersionInfoCompany=RaggieSoft
VersionInfoDescription={#MyAppName} 2.0 Setup
VersionInfoCopyright=Copyright (C) Michael P. Ragsdale (MIT / CC BY-SA 4.0)
WizardImageFile=Assets\WizardBanner-StoreTrainer.bmp
WizardSmallImageFile=Assets\WizardSmall-StoreTrainer.bmp
WizardImageStretch=yes
WizardStyle=modern
WizardSizePercent=120,115
WizardResizable=yes
DisableWelcomePage=no
Compression=lzma2/ultra64
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "associate_stf"; Description: "Associate .stf StoreFile profiles with {#MyAppName} (double-click to launch, right-click to edit)"; GroupDescription: "File Associations:"

[Dirs]
Name: "{userappdata}\RaggieSoft\StoreTrainer"
Name: "{userdocs}\My StoreFiles"

; --------------------------------------------------------------------------------
; [InstallDelete] SECTION: Clean up legacy All-Users NSIS Start Menu shortcut
; --------------------------------------------------------------------------------
[InstallDelete]
; Classic NSIS 'TSSetup.nsi' placed 'StoreTrainer.lnk' at the root of All Users Programs
Type: files; Name: "{commonprograms}\StoreTrainer.lnk"

; --------------------------------------------------------------------------------
; [Files] SECTION: Install CPU-matched native x64 or ARM64 binaries into '{app}'
; --------------------------------------------------------------------------------
[Files]
Source: "{#BuildDirX64}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: not IsArm64
Source: "{#BuildDirArm64}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: IsArm64

; --------------------------------------------------------------------------------
; [Icons] SECTION: Start Menu and optional Desktop shortcuts
; --------------------------------------------------------------------------------
[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "Organize and launch PC games alongside their companion trainers"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "Organize and launch PC games alongside their companion trainers"; Tasks: desktopicon

; --------------------------------------------------------------------------------
; [Registry] SECTION:
;   1. Declaratively remove legacy 32-bit NSIS Uninstall & App keys (if Admin install)
;      so Windows "Installed Apps" has ONE entry owned by Inno Setup's unins000.exe!
;   2. Register '.stf' StoreFile association ('open' -> '/l "%1"', 'edit' -> '/e "%1"').
;   3. Register 'raggiesoft-storetrainer://' custom URL protocol.
; --------------------------------------------------------------------------------
[Registry]
; Remove legacy 32-bit NSIS Programs & Features entry (plus v1.0's historical 'Microsoft Windows' typo key)
Root: HKLM32; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\RaggieSoft StoreTrainer"; Flags: deletekey noerror; Check: IsAdminInstallMode
Root: HKLM32; Subkey: "Software\Microsoft Windows\CurrentVersion\Uninstall\RaggieSoft StoreTrainer"; Flags: deletekey noerror; Check: IsAdminInstallMode
Root: HKLM32; Subkey: "Software\RaggieSoft\StoreTrainer"; Flags: deletekey noerror; Check: IsAdminInstallMode

; Register '.stf' file extension -> 'RaggieSoft.StoreFile' ProgID
Root: HKA; Subkey: "Software\Classes\.stf"; ValueType: string; ValueName: ""; ValueData: "RaggieSoft.StoreFile"; Flags: uninsdeletevalue; Tasks: associate_stf
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFile"; ValueType: string; ValueName: ""; ValueData: "RaggieSoft StoreTrainer Profile"; Flags: uninsdeletekey; Tasks: associate_stf
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"",0"; Tasks: associate_stf
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFile\shell\open"; ValueType: string; ValueName: ""; ValueData: "Launch Game && Trainer"; Tasks: associate_stf
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" /l ""%1"""; Tasks: associate_stf
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFile\shell\edit"; ValueType: string; ValueName: ""; ValueData: "Edit in RaggieSoft StoreTrainer"; Tasks: associate_stf
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFile\shell\edit\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" /e ""%1"""; Tasks: associate_stf

; Register 'raggiesoft-storetrainer://' deep-link protocol
Root: HKA; Subkey: "Software\Classes\raggiesoft-storetrainer"; ValueType: string; ValueName: ""; ValueData: "URL:RaggieSoft StoreTrainer Protocol"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\raggiesoft-storetrainer"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\raggiesoft-storetrainer\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"",0"
Root: HKA; Subkey: "Software\Classes\raggiesoft-storetrainer\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; --------------------------------------------------------------------------------
; [Run] SECTION: Install .NET 10 Desktop Runtime (only if missing) & optional Launch
; --------------------------------------------------------------------------------
[Run]
Filename: "{tmp}\{code:GetDotNetInstallerFileName}"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing Microsoft .NET 10.0 Desktop Runtime..."; Flags: waituntilterminated; Check: ShouldInstallDownloadedDotNet10Runtime
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

; --------------------------------------------------------------------------------
; [Code] SECTION: Pascal Script hooks (Surgical NSIS Upgrade, .NET 10 Check & Uninstall UI)
; --------------------------------------------------------------------------------
[Code]
var
  DownloadPage: TDownloadWizardPage;
  DotNet10RuntimeDownloaded: Boolean;
  LegacyNsisInstallDir: String;

  // Custom Uninstaller Wizard Page controls & flags (default False / unchecked for safety!)
  UninstallOptionsPage: TNewNotebookPage;
  RemoveProfilesCheckBox: TNewCheckBox;
  RemovePrefsCheckBox: TNewCheckBox;
  UninstallRemoveProfiles: Boolean;
  UninstallRemovePrefs: Boolean;

// ============================================================================
// SURGICAL LEGACY NSIS (v1.0 / v1.1) DISCOVERY & CLEANUP
//   - Captured BEFORE [Registry] deletes 'HKLM32\Software\RaggieSoft\StoreTrainer'.
//   - Surgically removes ONLY StoreTrainer's own legacy files from 'Program Files (x86)',
//     leaving C:\Windows\SysWOW64\ VB6 runtime DLLs/OCXs and user '.stf' files 100% intact!
// ============================================================================
function DetectLegacyNsisInstallFolder(): String;
var
  DirVal, UninsVal: String;
begin
  Result := '';
  if RegQueryStringValue(HKLM32, 'Software\RaggieSoft\StoreTrainer', 'Installation Folder', DirVal) and (Trim(DirVal) <> '') then
  begin
    Result := RemoveBackslashUnlessRoot(Trim(DirVal));
    Exit;
  end;

  if RegQueryStringValue(HKLM32, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\RaggieSoft StoreTrainer', 'UninstallString', UninsVal) and (Trim(UninsVal) <> '') then
  begin
    Result := RemoveBackslashUnlessRoot(ExtractFileDir(RemoveQuotes(Trim(UninsVal))));
    Exit;
  end;
end;

procedure PerformSurgicalLegacyNsisCleanup();
var
  ParentDir: String;
begin
  if (LegacyNsisInstallDir = '') or (not DirExists(LegacyNsisInstallDir)) then
    Exit;

  // Safety check: only clean if this folder actually contains classic TStore.exe or NSIS uninstall.exe
  if (not FileExists(LegacyNsisInstallDir + '\TStore.exe')) and (not FileExists(LegacyNsisInstallDir + '\uninstall.exe')) then
    Exit;

  Log('Upgrading classic NSIS StoreTrainer installation at: ' + LegacyNsisInstallDir);

  // Remove optional VB6 Source\ subfolder installed by TSSetup.nsi
  if DirExists(LegacyNsisInstallDir + '\Source') then
    DelTree(LegacyNsisInstallDir + '\Source', True, True, True);

  // Surgically remove ONLY the exact files placed into $INSTDIR by TSSetup.nsi / TSSetup-Patch.nsi
  DeleteFile(LegacyNsisInstallDir + '\TStore.exe');
  DeleteFile(LegacyNsisInstallDir + '\TStore.exe.manifest');
  DeleteFile(LegacyNsisInstallDir + '\LICENSE.rtf');
  DeleteFile(LegacyNsisInstallDir + '\License.rtf');
  DeleteFile(LegacyNsisInstallDir + '\README.txt');
  DeleteFile(LegacyNsisInstallDir + '\TStore.ini');
  DeleteFile(LegacyNsisInstallDir + '\uninstall.exe');

  // RemoveDir (unlike DelTree) removes a folder ONLY IF IT IS COMPLETELY EMPTY —
  // guaranteeing we never touch any user-added files if the user placed extras inside!
  ParentDir := ExtractFileDir(LegacyNsisInstallDir);
  RemoveDir(LegacyNsisInstallDir);
  if ParentDir <> '' then
    RemoveDir(ParentDir);
end;

// ============================================================================
// .NET 10.0 DESKTOP RUNTIME DETECTION & ON-DEMAND DOWNLOAD
// ============================================================================
function GetDotNetInstallerUrl(): String;
begin
  if IsArm64 then
    Result := '{#DotNet10DesktopArm64Url}'
  else
    Result := '{#DotNet10DesktopX64Url}';
end;

function GetDotNetInstallerFileName(Param: String): String;
begin
  if IsArm64 then
    Result := '{#DotNet10DesktopArm64File}'
  else
    Result := '{#DotNet10DesktopX64File}';
end;

function IsDotNet10DesktopInstalled(): Boolean;
var
  ValueNames: TArrayOfString;
  I: Integer;
  FindRec: TFindRec;
  ArchKey, SharedDir: String;
begin
  Result := False;
  if IsArm64 then
    ArchKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\arm64\sharedfx\Microsoft.WindowsDesktop.App'
  else
    ArchKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';

  if RegGetValueNames(HKLM64, ArchKey, ValueNames) then
  begin
    for I := 0 to GetArrayLength(ValueNames) - 1 do
    begin
      if Pos('10.0.', ValueNames[I]) = 1 then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;

  SharedDir := ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if FindFirst(SharedDir + '\10.0.*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Result := True;
          Exit;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

function ShouldInstallDownloadedDotNet10Runtime(): Boolean;
begin
  Result := DotNet10RuntimeDownloaded and FileExists(ExpandConstant('{tmp}\') + GetDotNetInstallerFileName(''));
end;

function InitializeSetup(): Boolean;
begin
  // Snapshot any legacy 32-bit NSIS path right at startup before [Registry] deletes the key!
  LegacyNsisInstallDir := DetectLegacyNsisInstallFolder();
  Result := True;
end;

procedure InitializeWizard;
begin
  DotNet10RuntimeDownloaded := False;
  DownloadPage := CreateDownloadPage(
    SetupMessage(msgWizardPreparing),
    'Downloading required Microsoft .NET 10 Desktop Runtime...',
    nil);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if (CurPageID = wpReady) and (not IsDotNet10DesktopInstalled()) then
  begin
    DownloadPage.Clear;
    DownloadPage.Add(GetDotNetInstallerUrl(), GetDotNetInstallerFileName(''), '');
    DownloadPage.Show;
    try
      try
        DownloadPage.Download;
        if FileExists(ExpandConstant('{tmp}\') + GetDotNetInstallerFileName('')) then
          DotNet10RuntimeDownloaded := True;
      except
        if not DownloadPage.AbortedByUser then
          SuppressibleMsgBox(
            'Unable to download Microsoft .NET 10 Desktop Runtime automatically: ' + AddPeriod(GetExceptionMessage),
            mbCriticalError, MB_OK, IDOK);
      end;
    finally
      DownloadPage.Hide;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    // Perform the surgical NSIS v1.0 / v1.1 cleanup right before extracting v2.0 files
    if IsAdminInstallMode then
      PerformSurgicalLegacyNsisCleanup();
  end;
end;

// ============================================================================
// CUSTOM UNINSTALLER OPTIONS PAGE (BOTH UNCHECKED BY DEFAULT FOR USER SAFETY)
// ============================================================================
procedure InitializeUninstallProgressForm();
var
  IntroLabel, ProfilesNoteLabel, PrefsNoteLabel: TNewStaticText;
  UninstallNextButton: TNewButton;
  OrigPageName, OrigPageDesc: String;
  OrigCancelEnabled: Boolean;
begin
  UninstallRemoveProfiles := False;
  UninstallRemovePrefs := False;

  if UninstallSilent then
    Exit;

  OrigPageName := UninstallProgressForm.PageNameLabel.Caption;
  OrigPageDesc := UninstallProgressForm.PageDescriptionLabel.Caption;
  OrigCancelEnabled := UninstallProgressForm.CancelButton.Enabled;

  UninstallProgressForm.PageNameLabel.Caption := 'Uninstall Options — Game Profiles & Settings';
  UninstallProgressForm.PageDescriptionLabel.Caption :=
    'Choose whether to keep or remove your saved My StoreFiles game profiles and preferences.';

  UninstallOptionsPage := TNewNotebookPage.Create(UninstallProgressForm);
  UninstallOptionsPage.Notebook := UninstallProgressForm.InnerNotebook;
  UninstallOptionsPage.Parent := UninstallProgressForm.InnerNotebook;
  UninstallOptionsPage.BoundsRect := UninstallProgressForm.InstallingPage.BoundsRect;

  IntroLabel := TNewStaticText.Create(UninstallProgressForm);
  IntroLabel.Parent := UninstallOptionsPage;
  IntroLabel.Left := ScaleX(0);
  IntroLabel.Top := ScaleY(0);
  IntroLabel.Width := UninstallOptionsPage.Width;
  IntroLabel.WordWrap := True;
  IntroLabel.AutoSize := True;
  IntroLabel.Caption :=
    'Setup will now remove {#MyAppName} from your computer. By default, your saved game profiles ' +
    'in Documents\My StoreFiles and personal settings are kept safe.';

  RemoveProfilesCheckBox := TNewCheckBox.Create(UninstallProgressForm);
  RemoveProfilesCheckBox.Parent := UninstallOptionsPage;
  RemoveProfilesCheckBox.Left := ScaleX(0);
  RemoveProfilesCheckBox.Top := IntroLabel.Top + IntroLabel.Height + ScaleY(18);
  RemoveProfilesCheckBox.Width := UninstallOptionsPage.Width;
  RemoveProfilesCheckBox.Height := ScaleY(22);
  RemoveProfilesCheckBox.Caption := 'Remove my saved game profiles in Documents\My StoreFiles';
  RemoveProfilesCheckBox.Checked := False;

  ProfilesNoteLabel := TNewStaticText.Create(UninstallProgressForm);
  ProfilesNoteLabel.Parent := UninstallOptionsPage;
  ProfilesNoteLabel.Left := ScaleX(22);
  ProfilesNoteLabel.Top := RemoveProfilesCheckBox.Top + RemoveProfilesCheckBox.Height + ScaleY(2);
  ProfilesNoteLabel.Width := UninstallOptionsPage.Width - ScaleX(22);
  ProfilesNoteLabel.WordWrap := True;
  ProfilesNoteLabel.AutoSize := True;
  ProfilesNoteLabel.Caption :=
    'Leave unchecked to keep all of your .stf game and trainer profiles (your actual games are never touched).';

  RemovePrefsCheckBox := TNewCheckBox.Create(UninstallProgressForm);
  RemovePrefsCheckBox.Parent := UninstallOptionsPage;
  RemovePrefsCheckBox.Left := ScaleX(0);
  RemovePrefsCheckBox.Top := ProfilesNoteLabel.Top + ProfilesNoteLabel.Height + ScaleY(16);
  RemovePrefsCheckBox.Width := UninstallOptionsPage.Width;
  RemovePrefsCheckBox.Height := ScaleY(22);
  RemovePrefsCheckBox.Caption := 'Remove saved {#MyAppName} preferences';
  RemovePrefsCheckBox.Checked := False;

  PrefsNoteLabel := TNewStaticText.Create(UninstallProgressForm);
  PrefsNoteLabel.Parent := UninstallOptionsPage;
  PrefsNoteLabel.Left := ScaleX(22);
  PrefsNoteLabel.Top := RemovePrefsCheckBox.Top + RemovePrefsCheckBox.Height + ScaleY(2);
  PrefsNoteLabel.Width := UninstallOptionsPage.Width - ScaleX(22);
  PrefsNoteLabel.WordWrap := True;
  PrefsNoteLabel.AutoSize := True;
  PrefsNoteLabel.Caption :=
    'Leave unchecked to preserve your library path and theme preference for future updates.';

  UninstallProgressForm.InnerNotebook.ActivePage := UninstallOptionsPage;

  UninstallNextButton := TNewButton.Create(UninstallProgressForm);
  UninstallNextButton.Parent := UninstallProgressForm;
  UninstallNextButton.Left :=
    UninstallProgressForm.CancelButton.Left - UninstallProgressForm.CancelButton.Width - ScaleX(10);
  UninstallNextButton.Top := UninstallProgressForm.CancelButton.Top;
  UninstallNextButton.Width := UninstallProgressForm.CancelButton.Width;
  UninstallNextButton.Height := UninstallProgressForm.CancelButton.Height;
  UninstallNextButton.Caption := '&Uninstall';
  UninstallNextButton.ModalResult := mrOk;
  UninstallNextButton.Default := True;
  UninstallNextButton.TabOrder := 0;

  UninstallProgressForm.CancelButton.Enabled := True;
  UninstallProgressForm.CancelButton.ModalResult := mrCancel;
  UninstallProgressForm.CancelButton.Cancel := True;
  UninstallProgressForm.CancelButton.TabOrder := 1;

  if UninstallProgressForm.ShowModal <> mrOk then
    Abort;

  UninstallRemoveProfiles := RemoveProfilesCheckBox.Checked;
  UninstallRemovePrefs := RemovePrefsCheckBox.Checked;

  UninstallNextButton.Visible := False;
  UninstallProgressForm.PageNameLabel.Caption := OrigPageName;
  UninstallProgressForm.PageDescriptionLabel.Caption := OrigPageDesc;
  UninstallProgressForm.CancelButton.Enabled := OrigCancelEnabled;
  UninstallProgressForm.CancelButton.ModalResult := mrNone;
  UninstallProgressForm.InnerNotebook.ActivePage := UninstallProgressForm.InstallingPage;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  StoreFilesDir, StoreTrainerAppDataDir: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    StoreFilesDir := ExpandConstant('{userdocs}\My StoreFiles');
    StoreTrainerAppDataDir := ExpandConstant('{userappdata}\RaggieSoft\StoreTrainer');

    if UninstallRemoveProfiles and DirExists(StoreFilesDir) then
      DelTree(StoreFilesDir, True, True, True);

    if UninstallRemovePrefs and DirExists(StoreTrainerAppDataDir) then
    begin
      DelTree(StoreTrainerAppDataDir, True, True, True);
      RemoveDir(ExpandConstant('{userappdata}\RaggieSoft'));
    end;
  end;
end;
