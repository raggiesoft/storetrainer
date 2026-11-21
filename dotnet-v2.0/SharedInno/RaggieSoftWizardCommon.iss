; ================================================================================
; FILE: RaggieSoftWizardCommon.iss
; LOCATION: SharedInno\RaggieSoftWizardCommon.iss
; INCLUDED BY: StoreTrainer.iss, BookReader.iss, ListeningRoom.iss,
;              Backstage.iss, and RaggieSoftLauncher.iss
; LICENSE: MIT License (Code)
;
; PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
;   What is this shared '.iss' include file?
;     Instead of copy-pasting the same setup customization logic into five separate
;     Inno Setup scripts, every RaggieSoft '.iss' script pulls this file in with:
;       #include "..\SharedInno\RaggieSoftWizardCommon.iss"
;
;   What four suite-wide capabilities live here?
;     1. Automatic Packaging of 'RaggieSoftUpdate.exe' (Zero-Runtime C++20 Updater):
;        If 'RaggieSoftUpdate.exe' has been compiled in '..\RaggieSoftUpdate\bin\...\Release\',
;        every RaggieSoft installer bundles it into '{autopf}\RaggieSoft\Update\' and
;        registers 'HKA\Software\RaggieSoft\Update\ExecutablePath' so 'Check for Updates'
;        works immediately whether the user installed one app or the whole suite!
;
;     2. Express vs. Custom Installation Mode Wizard Page:
;        Right after the License screen, offers a clean 2-option radio choice:
;          (*) Express Setup (Recommended) — One-click setup using sensible defaults
;              (automatically skips Destination Folder, Start Menu Folder, and Tasks!).
;          ( ) Custom Setup — Choose your installation folder, Start Menu & Desktop
;              shortcuts, and file/protocol preferences step by step.
;        Also honors CLI flags '/SETUPTYPE=EXPRESS' or '/SETUPTYPE=CUSTOM'.
;
;     3. Custom Ready-to-Install Summary Builder ('FormatRaggieSoftReadyMemo'):
;        Displays a crisp human-readable summary on 'wpReady' showing Setup Mode,
;        Destination, Shortcuts, Associations, and whether Microsoft .NET 10 Desktop
;        Runtime is already installed on this PC!
;
;     4. Multi-User HKCU Classes Cleanup via Windows 'Active Setup':
;        When an Administrator uninstalls an app on a shared family/work PC, other
;        user accounts on that PC cannot have their 'HKCU\Software\Classes\...' hives
;        edited while they are signed out. Writing a tiny one-shot 'reg.exe delete'
;        entry under 'HKLM\SOFTWARE\Microsoft\Active Setup\Installed Components' tells
;        Windows Explorer to clean up any stale per-user association automatically the
;        next time each other user signs in — zero separate "Removal Tool" needed!
; ================================================================================

; --------------------------------------------------------------------------------
; [Files] & [Registry]: Bundle RaggieSoftUpdate.exe automatically whenever compiled
; --------------------------------------------------------------------------------
#define SharedUpdaterX64 "..\RaggieSoftUpdate\bin\x64\Release\RaggieSoftUpdate.exe"
#define SharedUpdaterArm64 "..\RaggieSoftUpdate\bin\ARM64\Release\RaggieSoftUpdate.exe"

[Files]
#if FileExists(SharedUpdaterX64)
Source: "{#SharedUpdaterX64}"; DestDir: "{autopf}\RaggieSoft\Update"; Flags: ignoreversion uninsneveruninstall; Check: not IsArm64
#endif
#if FileExists(SharedUpdaterArm64)
Source: "{#SharedUpdaterArm64}"; DestDir: "{autopf}\RaggieSoft\Update"; Flags: ignoreversion uninsneveruninstall; Check: IsArm64
#endif

[Registry]
#if FileExists(SharedUpdaterX64) || FileExists(SharedUpdaterArm64)
Root: HKA; Subkey: "Software\RaggieSoft\Update"; ValueType: string; ValueName: "ExecutablePath"; ValueData: "{autopf}\RaggieSoft\Update\RaggieSoftUpdate.exe"
#endif

; --------------------------------------------------------------------------------
; [Code]: Express vs. Custom Wizard Page, Ready Memo & Multi-User Active Setup Stub
; --------------------------------------------------------------------------------
[Code]
var
  RaggieSoftInstallModePage: TWizardPage;
  ExpressModeRadio: TNewRadioButton;
  CustomModeRadio: TNewRadioButton;

// Checks CLI for '/SETUPTYPE=CUSTOM' so power users can pre-select Custom mode
function HasCustomSetupTypeCliFlag(): Boolean;
var
  I: Integer;
  UpperArg: String;
begin
  Result := False;
  for I := 1 to ParamCount do
  begin
    UpperArg := Uppercase(Trim(ParamStr(I)));
    if (UpperArg = '/SETUPTYPE=CUSTOM') or (UpperArg = '/CUSTOM') then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

// Creates the Express vs. Custom Installation Mode radio page right after wpLicense
procedure InitRaggieSoftExpressCustomPage(const ExpressSummaryLine, CustomSummaryLine: String);
var
  IntroText, ExpressDesc, CustomDesc: TNewStaticText;
  DefaultCustom: Boolean;
begin
  DefaultCustom := HasCustomSetupTypeCliFlag();

  RaggieSoftInstallModePage := CreateCustomPage(
    wpLicense,
    'Choose Installation Type',
    'Select Express Setup for recommended defaults, or Custom Setup to tailor folders and shortcuts.');

  IntroText := TNewStaticText.Create(RaggieSoftInstallModePage);
  IntroText.Parent := RaggieSoftInstallModePage.Surface;
  IntroText.Left := ScaleX(0);
  IntroText.Top := ScaleY(0);
  IntroText.Width := RaggieSoftInstallModePage.SurfaceWidth;
  IntroText.WordWrap := True;
  IntroText.AutoSize := True;
  IntroText.Caption := 'How would you like to install {#MyAppName}?';

  ExpressModeRadio := TNewRadioButton.Create(RaggieSoftInstallModePage);
  ExpressModeRadio.Parent := RaggieSoftInstallModePage.Surface;
  ExpressModeRadio.Left := ScaleX(0);
  ExpressModeRadio.Top := IntroText.Top + IntroText.Height + ScaleY(14);
  ExpressModeRadio.Width := RaggieSoftInstallModePage.SurfaceWidth;
  ExpressModeRadio.Height := ScaleY(22);
  ExpressModeRadio.Font.Style := [fsBold];
  ExpressModeRadio.Caption := 'Express Setup (Recommended)';
  ExpressModeRadio.Checked := not DefaultCustom;

  ExpressDesc := TNewStaticText.Create(RaggieSoftInstallModePage);
  ExpressDesc.Parent := RaggieSoftInstallModePage.Surface;
  ExpressDesc.Left := ScaleX(22);
  ExpressDesc.Top := ExpressModeRadio.Top + ExpressModeRadio.Height + ScaleY(2);
  ExpressDesc.Width := RaggieSoftInstallModePage.SurfaceWidth - ScaleX(22);
  ExpressDesc.WordWrap := True;
  ExpressDesc.AutoSize := True;
  ExpressDesc.Caption := ExpressSummaryLine;

  CustomModeRadio := TNewRadioButton.Create(RaggieSoftInstallModePage);
  CustomModeRadio.Parent := RaggieSoftInstallModePage.Surface;
  CustomModeRadio.Left := ScaleX(0);
  CustomModeRadio.Top := ExpressDesc.Top + ExpressDesc.Height + ScaleY(18);
  CustomModeRadio.Width := RaggieSoftInstallModePage.SurfaceWidth;
  CustomModeRadio.Height := ScaleY(22);
  CustomModeRadio.Font.Style := [fsBold];
  CustomModeRadio.Caption := 'Custom Setup';
  CustomModeRadio.Checked := DefaultCustom;

  CustomDesc := TNewStaticText.Create(RaggieSoftInstallModePage);
  CustomDesc.Parent := RaggieSoftInstallModePage.Surface;
  CustomDesc.Left := ScaleX(22);
  CustomDesc.Top := CustomModeRadio.Top + CustomModeRadio.Height + ScaleY(2);
  CustomDesc.Width := RaggieSoftInstallModePage.SurfaceWidth - ScaleX(22);
  CustomDesc.WordWrap := True;
  CustomDesc.AutoSize := True;
  CustomDesc.Caption := CustomSummaryLine;
end;

function IsRaggieSoftExpressMode(): Boolean;
begin
  Result := (ExpressModeRadio <> nil) and ExpressModeRadio.Checked;
end;

// Skips folder/group/tasks pages automatically when Express Setup is chosen
function ShouldSkipStandardPagesInExpressMode(const PageID: Integer): Boolean;
begin
  Result := False;
  if IsRaggieSoftExpressMode() then
  begin
    if (PageID = wpSelectDir) or (PageID = wpSelectProgramGroup) or (PageID = wpSelectTasks) then
      Result := True;
  end;
end;

// Formats a clean Ready-to-Install overview memo (including .NET 10 runtime status!)
function FormatRaggieSoftReadyMemo(
  const Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo,
  MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo, ExtraCustomLines: String;
  const DotNetAlreadyPresent: Boolean): String;
var
  S: String;
begin
  S := 'Installation Mode:' + NewLine + Space;
  if IsRaggieSoftExpressMode() then
    S := S + 'Express Setup (Recommended defaults)' + NewLine + NewLine
  else
    S := S + 'Custom Setup' + NewLine + NewLine;

  if MemoDirInfo <> '' then
    S := S + MemoDirInfo + NewLine + NewLine;

  if MemoGroupInfo <> '' then
    S := S + MemoGroupInfo + NewLine + NewLine;

  if MemoTasksInfo <> '' then
    S := S + MemoTasksInfo + NewLine + NewLine;

  if ExtraCustomLines <> '' then
    S := S + ExtraCustomLines + NewLine + NewLine;

  S := S + 'Runtime Status:' + NewLine + Space;
  if DotNetAlreadyPresent then
    S := S + 'Microsoft .NET 10 Desktop Runtime is already installed (no extra download needed).'
  else
    S := S + 'Microsoft .NET 10 Desktop Runtime will be downloaded and installed automatically.';

  Result := S;
end;

// Registers a lightweight Windows Active Setup stub on Admin Uninstall so any OTHER
// user accounts on the same PC automatically clean up stale per-user HKCU\Software\Classes
// entries the next time they sign into Windows (zero external removal tool required!).
procedure RegisterMultiUserHkcuClassesCleanupStub(const ComponentGuid, ClassSubKeyName: String);
var
  ActiveSetupKey, CmdLine: String;
begin
  if not IsAdminInstallMode then
    Exit;

  ActiveSetupKey := 'SOFTWARE\Microsoft\Active Setup\Installed Components\' + ComponentGuid;
  CmdLine := ExpandConstant('{sys}\reg.exe') + ' delete "HKCU\Software\Classes\' + ClassSubKeyName + '" /f';

  RegWriteStringValue(HKLM64, ActiveSetupKey, '', '{#MyAppName} Per-User Cleanup');
  RegWriteStringValue(HKLM64, ActiveSetupKey, 'StubPath', CmdLine);
  RegWriteStringValue(HKLM64, ActiveSetupKey, 'Version', '2,0,' + GetDateTimeString('yyyymmdd,hhnnss', '', ''));
end;
