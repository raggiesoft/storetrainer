; ================================================================================
; FILE: StoreTrainer.iss
; APPLICATION: RaggieSoft StoreTrainer 2.0
; COMPILER: Inno Setup 7.1.1 (Native 64-Bit ISCC.exe at C:\Program Files\Inno Setup 7)
; ARCHITECTURES: Native AMD64 (win-x64) & Native ARM64 (win-arm64)
; LICENSE: MIT License (Application Code) / CC BY-SA 4.0 (Documentation & Artwork)
;
; PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
;   What is this file?
;     This '.iss' file compiles 'RaggieSoft-StoreTrainer-Setup.exe' inside '..\Installers\'.
;     It brings together:
;       1. Express vs. Custom Installation Mode (via 'SharedInno\RaggieSoftWizardCommon.iss'),
;          plus a Custom Setup page choosing whether double-clicking a '.stfx' file
;          launches your game & trainer immediately ('/l') or opens the editor ('/e')!
;       2. Modern '.stfx' ('Documents\StoreFiles\') and optional classic '.stf' file
;          associations, plus 'raggiesoft-storetrainer://' deep links and automatic
;          deployment of our zero-runtime C++20 updater ('RaggieSoftUpdate.exe').
;       3. Surgical NSIS-to-Inno Setup Upgrade (v1.0 / v1.1 -> v2.0):
;          Never touches 'C:\Windows\SysWOW64\' VB6 runtime files or 'Documents\My StoreFiles\*.stf';
;          surgically removes only StoreTrainer's legacy 32-bit files and Uninstall key.
;       4. Multi-User 'HKCU\Software\Classes' Cleanup via Windows 'Active Setup' on
;          Admin uninstall so other user accounts on a shared PC never need an external
;          "File Associations Removal Tool"!
; ================================================================================

#ifndef BuildDirX64
  #define BuildDirX64 "bin\Release\net10.0-windows\win-x64\publish"
#endif
#ifndef BuildDirArm64
  #define BuildDirArm64 "bin\Release\net10.0-windows\win-arm64\publish"
#endif

#define MyAppName "RaggieSoft StoreTrainer"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "RaggieSoft (Michael P. Ragsdale)"
#define MyAppURL "https://storetrainer.raggiesoft.com"
#define MyAppExeName "StoreTrainer.exe"
#define MyAppMutex "RaggieSoft_StoreTrainer_Mutex"

#define DotNet10DesktopX64Url "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe"
#define DotNet10DesktopX64File "windowsdesktop-runtime-10.0-win-x64.exe"
#define DotNet10DesktopArm64Url "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-arm64.exe"
#define DotNet10DesktopArm64File "windowsdesktop-runtime-10.0-win-arm64.exe"

[Setup]
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
Name: "associate_stfx"; Description: "Register .stfx StoreTrainer 2.0 game profiles in File Explorer"; GroupDescription: "File Associations:"
Name: "associate_stf"; Description: "Open classic .stf StoreFiles (v1.0 / v1.1) in the StoreTrainer 2.0 Upgrade Wizard"; GroupDescription: "File Associations:"

[Dirs]
Name: "{userappdata}\RaggieSoft"
Name: "{userdocs}\StoreFiles"

[InstallDelete]
; Remove legacy All-Users NSIS Start Menu shortcut from v1.0 / v1.1
Type: files; Name: "{commonprograms}\StoreTrainer.lnk"

[Files]
Source: "{#BuildDirX64}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: not IsArm64
Source: "{#BuildDirArm64}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: IsArm64

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "Organize and launch PC games alongside their companion trainers"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "Organize and launch PC games alongside their companion trainers"; Tasks: desktopicon

[Registry]
; Clean up legacy 32-bit NSIS Uninstall & App keys when installing in Admin mode
Root: HKLM32; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\RaggieSoft StoreTrainer"; Flags: deletekey noerror; Check: IsAdminInstallMode
Root: HKLM32; Subkey: "Software\Microsoft Windows\CurrentVersion\Uninstall\RaggieSoft StoreTrainer"; Flags: deletekey noerror; Check: IsAdminInstallMode
Root: HKLM32; Subkey: "Software\RaggieSoft\StoreTrainer"; Flags: deletekey noerror; Check: IsAdminInstallMode

; Modern '.stfx' JSON profile association (respects Custom Setup's Launch vs. Edit double-click verb!)
Root: HKA; Subkey: "Software\Classes\.stfx"; ValueType: string; ValueName: ""; ValueData: "RaggieSoft.StoreFileX"; Flags: uninsdeletevalue; Tasks: associate_stfx
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFileX"; ValueType: string; ValueName: ""; ValueData: "RaggieSoft StoreTrainer 2.0 Profile"; Flags: uninsdeletekey; Tasks: associate_stfx
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFileX\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"",0"; Tasks: associate_stfx
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFileX\shell"; ValueType: string; ValueName: ""; ValueData: "{code:GetStoreFileDefaultVerb}"; Tasks: associate_stfx
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFileX\shell\open"; ValueType: string; ValueName: ""; ValueData: "Launch Game && Trainer"; Tasks: associate_stfx
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFileX\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" /l ""%1"""; Tasks: associate_stfx
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFileX\shell\edit"; ValueType: string; ValueName: ""; ValueData: "Edit in RaggieSoft StoreTrainer"; Tasks: associate_stfx
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFileX\shell\edit\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" /e ""%1"""; Tasks: associate_stfx

; Optional classic '.stf' association -> opens StoreTrainer 2.0's non-destructive Upgrade Wizard
Root: HKA; Subkey: "Software\Classes\.stf"; ValueType: string; ValueName: ""; ValueData: "RaggieSoft.StoreFile"; Flags: uninsdeletevalue; Tasks: associate_stf
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFile"; ValueType: string; ValueName: ""; ValueData: "Classic RaggieSoft StoreFile (v1.0 / v1.1)"; Flags: uninsdeletekey; Tasks: associate_stf
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"",0"; Tasks: associate_stf
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFile\shell\open"; ValueType: string; ValueName: ""; ValueData: "Upgrade in RaggieSoft StoreTrainer 2.0"; Tasks: associate_stf
Root: HKA; Subkey: "Software\Classes\RaggieSoft.StoreFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" /e ""%1"""; Tasks: associate_stf

; Register 'raggiesoft-storetrainer://' deep-link protocol
Root: HKA; Subkey: "Software\Classes\raggiesoft-storetrainer"; ValueType: string; ValueName: ""; ValueData: "URL:RaggieSoft StoreTrainer Protocol"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\raggiesoft-storetrainer"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\raggiesoft-storetrainer\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"",0"
Root: HKA; Subkey: "Software\Classes\raggiesoft-storetrainer\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Run]
Filename: "{tmp}\{code:GetDotNetInstallerFileName}"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing Microsoft .NET 10.0 Desktop Runtime..."; Flags: waituntilterminated; Check: ShouldInstallDownloadedDotNet10Runtime
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

; Pull in shared RaggieSoftUpdate.exe packaging, Express/Custom page & Active Setup multi-user cleanup!
#include "..\SharedInno\RaggieSoftWizardCommon.iss"

[Code]
var
  DownloadPage: TDownloadWizardPage;
  DoubleClickVerbPage: TInputOptionWizardPage;
  DotNet10RuntimeDownloaded: Boolean;
  LegacyNsisInstallDir: String;

  UninstallOptionsPage: TNewNotebookPage;
  RemoveProfilesCheckBox: TNewCheckBox;
  RemovePrefsCheckBox: TNewCheckBox;
  CleanOtherUsersCheckBox: TNewCheckBox;
  UninstallRemoveProfiles: Boolean;
  UninstallRemovePrefs: Boolean;
  UninstallCleanOtherUsers: Boolean;

function GetStoreFileDefaultVerb(Param: String): String;
begin
  if (DoubleClickVerbPage <> nil) and (not IsRaggieSoftExpressMode()) and (DoubleClickVerbPage.SelectedValueIndex = 1) then
    Result := 'edit'
  else
    Result := 'open';
end;

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

  if (not FileExists(LegacyNsisInstallDir + '\TStore.exe')) and (not FileExists(LegacyNsisInstallDir + '\uninstall.exe')) then
    Exit;

  Log('Upgrading classic NSIS StoreTrainer installation at: ' + LegacyNsisInstallDir);

  if DirExists(LegacyNsisInstallDir + '\Source') then
    DelTree(LegacyNsisInstallDir + '\Source', True, True, True);

  DeleteFile(LegacyNsisInstallDir + '\TStore.exe');
  DeleteFile(LegacyNsisInstallDir + '\TStore.exe.manifest');
  DeleteFile(LegacyNsisInstallDir + '\LICENSE.rtf');
  DeleteFile(LegacyNsisInstallDir + '\License.rtf');
  DeleteFile(LegacyNsisInstallDir + '\README.txt');
  DeleteFile(LegacyNsisInstallDir + '\TStore.ini');
  DeleteFile(LegacyNsisInstallDir + '\uninstall.exe');

  ParentDir := ExtractFileDir(LegacyNsisInstallDir);
  RemoveDir(LegacyNsisInstallDir);
  if ParentDir <> '' then
    RemoveDir(ParentDir);
end;

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
  LegacyNsisInstallDir := DetectLegacyNsisInstallFolder();
  Result := True;
end;

procedure InitializeWizard;
begin
  DotNet10RuntimeDownloaded := False;

  // 1. Shared Express vs. Custom Installation Mode page
  InitRaggieSoftExpressCustomPage(
    'Installs {#MyAppName} into the default RaggieSoft folder, creates shortcuts, registers .stfx game profiles (double-click to launch), and preserves any classic My StoreFiles.',
    'Lets you pick a custom installation folder, choose Start Menu and Desktop shortcuts, toggle .stfx / .stf file associations, and pick your preferred double-click behavior.');

  // 2. Custom Setup Double-Click Action page (modeled on classic StoreTrainer IS2010PR Setup.Rul!)
  DoubleClickVerbPage := CreateInputOptionPage(
    wpSelectTasks,
    'StoreFile (.stfx) Double-Click Behavior',
    'Choose what happens when you double-click a saved .stfx game profile in File Explorer.',
    'Both "Launch Game & Trainer" and "Edit in RaggieSoft StoreTrainer" remain available anytime in the right-click menu:',
    True,
    False);
  DoubleClickVerbPage.Add('Launch my game and companion trainer immediately (Recommended)');
  DoubleClickVerbPage.Add('Open the profile inside the StoreTrainer editor window');
  DoubleClickVerbPage.SelectedValueIndex := 0;

  DownloadPage := CreateDownloadPage(
    SetupMessage(msgWizardPreparing),
    'Downloading required Microsoft .NET 10 Desktop Runtime...',
    nil);
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := ShouldSkipStandardPagesInExpressMode(PageID);
  if (not Result) and (PageID = DoubleClickVerbPage.ID) then
  begin
    if IsRaggieSoftExpressMode() or (not WizardIsTaskSelected('associate_stfx')) then
      Result := True;
  end;
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  Extra: String;
begin
  Extra := 'StoreFile Double-Click Action:' + NewLine + Space;
  if GetStoreFileDefaultVerb('') = 'edit' then
    Extra := Extra + 'Open profile in StoreTrainer editor (/e)'
  else
    Extra := Extra + 'Launch game & trainer immediately (/l)';

  if LegacyNsisInstallDir <> '' then
    Extra := Extra + NewLine + NewLine + 'Classic Upgrade:' + NewLine + Space +
      'Upgrading classic StoreTrainer installation at ' + LegacyNsisInstallDir + ' (preserving My StoreFiles).';

  Result := FormatRaggieSoftReadyMemo(
    Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo,
    MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo, Extra,
    IsDotNet10DesktopInstalled());
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
    if IsAdminInstallMode then
      PerformSurgicalLegacyNsisCleanup();
  end;
end;

procedure InitializeUninstallProgressForm();
var
  IntroLabel, ProfilesNoteLabel, PrefsNoteLabel: TNewStaticText;
  UninstallNextButton: TNewButton;
  OrigPageName, OrigPageDesc: String;
  OrigCancelEnabled: Boolean;
begin
  UninstallRemoveProfiles := False;
  UninstallRemovePrefs := False;
  UninstallCleanOtherUsers := IsAdminInstallMode;

  if UninstallSilent then
    Exit;

  OrigPageName := UninstallProgressForm.PageNameLabel.Caption;
  OrigPageDesc := UninstallProgressForm.PageDescriptionLabel.Caption;
  OrigCancelEnabled := UninstallProgressForm.CancelButton.Enabled;

  UninstallProgressForm.PageNameLabel.Caption := 'Uninstall Options — Game Profiles & Associations';
  UninstallProgressForm.PageDescriptionLabel.Caption :=
    'Choose whether to keep or remove your saved StoreFiles game profiles, settings, and multi-user associations.';

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
    'Setup will now remove {#MyAppName}. By default, your saved game profiles in Documents\StoreFiles ' +
    '(and any classic My StoreFiles) are kept safe.';

  RemoveProfilesCheckBox := TNewCheckBox.Create(UninstallProgressForm);
  RemoveProfilesCheckBox.Parent := UninstallOptionsPage;
  RemoveProfilesCheckBox.Left := ScaleX(0);
  RemoveProfilesCheckBox.Top := IntroLabel.Top + IntroLabel.Height + ScaleY(14);
  RemoveProfilesCheckBox.Width := UninstallOptionsPage.Width;
  RemoveProfilesCheckBox.Height := ScaleY(22);
  RemoveProfilesCheckBox.Caption := 'Remove my saved .stfx game profiles and save-game backups';
  RemoveProfilesCheckBox.Checked := False;

  ProfilesNoteLabel := TNewStaticText.Create(UninstallProgressForm);
  ProfilesNoteLabel.Parent := UninstallOptionsPage;
  ProfilesNoteLabel.Left := ScaleX(22);
  ProfilesNoteLabel.Top := RemoveProfilesCheckBox.Top + RemoveProfilesCheckBox.Height + ScaleY(2);
  ProfilesNoteLabel.Width := UninstallOptionsPage.Width - ScaleX(22);
  ProfilesNoteLabel.WordWrap := True;
  ProfilesNoteLabel.AutoSize := True;
  ProfilesNoteLabel.Caption :=
    'Leave unchecked to keep Documents\StoreFiles (classic Documents\My StoreFiles and your installed games are never touched).';

  RemovePrefsCheckBox := TNewCheckBox.Create(UninstallProgressForm);
  RemovePrefsCheckBox.Parent := UninstallOptionsPage;
  RemovePrefsCheckBox.Left := ScaleX(0);
  RemovePrefsCheckBox.Top := ProfilesNoteLabel.Top + ProfilesNoteLabel.Height + ScaleY(12);
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

  if IsAdminInstallMode then
  begin
    CleanOtherUsersCheckBox := TNewCheckBox.Create(UninstallProgressForm);
    CleanOtherUsersCheckBox.Parent := UninstallOptionsPage;
    CleanOtherUsersCheckBox.Left := ScaleX(0);
    CleanOtherUsersCheckBox.Top := PrefsNoteLabel.Top + PrefsNoteLabel.Height + ScaleY(12);
    CleanOtherUsersCheckBox.Width := UninstallOptionsPage.Width;
    CleanOtherUsersCheckBox.Height := ScaleY(22);
    CleanOtherUsersCheckBox.Caption := 'Also clean up per-user .stfx file associations for other Windows accounts on next sign-in';
    CleanOtherUsersCheckBox.Checked := True;
  end;

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
  if CleanOtherUsersCheckBox <> nil then
    UninstallCleanOtherUsers := CleanOtherUsersCheckBox.Checked;

  UninstallNextButton.Visible := False;
  UninstallProgressForm.PageNameLabel.Caption := OrigPageName;
  UninstallProgressForm.PageDescriptionLabel.Caption := OrigPageDesc;
  UninstallProgressForm.CancelButton.Enabled := OrigCancelEnabled;
  UninstallProgressForm.CancelButton.ModalResult := mrNone;
  UninstallProgressForm.InnerNotebook.ActivePage := UninstallProgressForm.InstallingPage;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  StoreFilesDir, SaveBackupsDir, RoamingDir, PrefsFile: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    StoreFilesDir := ExpandConstant('{userdocs}\StoreFiles');
    SaveBackupsDir := ExpandConstant('{localappdata}\RaggieSoft\Cache\StoreTrainer');
    RoamingDir := ExpandConstant('{userappdata}\RaggieSoft');
    PrefsFile := RoamingDir + '\storetrainer-preferences.json';

    if UninstallRemoveProfiles then
    begin
      if DirExists(StoreFilesDir) then
        DelTree(StoreFilesDir, True, True, True);
      if DirExists(SaveBackupsDir) then
        DelTree(SaveBackupsDir, True, True, True);
    end;

    if UninstallRemovePrefs and FileExists(PrefsFile) then
    begin
      DeleteFile(PrefsFile);
      RemoveDir(RoamingDir);
    end;

    // Clean current user's per-user classes if present, and register Active Setup stub for OTHER accounts!
    RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\.stfx');
    RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\RaggieSoft.StoreFileX');
    RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\raggiesoft-storetrainer');

    if UninstallCleanOtherUsers and IsAdminInstallMode then
    begin
      RegisterMultiUserHkcuClassesCleanupStub('{8E92D4A1-63F0-4C8B-9B7A-10164500B004}_STFX', 'RaggieSoft.StoreFileX');
      RegisterMultiUserHkcuClassesCleanupStub('{8E92D4A1-63F0-4C8B-9B7A-10164500B004}_EXT', '.stfx');
      RegisterMultiUserHkcuClassesCleanupStub('{8E92D4A1-63F0-4C8B-9B7A-10164500B004}_URI', 'raggiesoft-storetrainer');
    end;
  end;
end;
