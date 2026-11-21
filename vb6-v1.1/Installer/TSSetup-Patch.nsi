; ==============================================================================
; FILE: TSSetup-Patch.nsi
; APPLICATION: RaggieSoft StoreTrainer v1.1 In-Place Patcher (v1.0 -> v1.1 & v1.1.x)
; COMPILER: Nullsoft Scriptable Install System (NSIS 2.x or NSIS 3.x in ANSI Mode)
; TARGET OS: Windows 95, 98, ME, NT 4.0 SP3+, 2000 SP4, XP, Vista, 7, 8.1, 10, 11
; LICENSE: MIT License
;
; PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
;   What is this file?
;     While 'TSSetup.nsi' bundles ~4 MB of Visual Basic 6.0 / DCOM98 / WSH 5.6
;     redistributables for bare Windows 95/98 machines, 'TSSetup-Patch.nsi' builds
;     a tiny (~150 KB!) in-place patch installer ('tspatch110.exe') for anyone who
;     already has StoreTrainer v1.0 (or an earlier v1.1.x build) installed!
;
;   How does it leave room for future VB6 v1.1.x updates?
;     1. Reads the exact version straight out of '..\Build\TStore.exe' at compile
;        time via '!getdllversion' and compares it dynamically against the user's
;        installed '$INSTDIR\TStore.exe' via '${VersionCompare}'.
;     2. Supports overriding the output filename from the CLI without editing code:
;          makensis /DPATCH_OUTFILE=tspatch111.exe TSSetup-Patch.nsi
;     3. Upgrades v1.0's Registry keys in place so RaggieSoft Launcher immediately
;        discovers the newly patched VB6 v1.1 installation!
; ==============================================================================

; Force 32-bit ANSI stub so this patcher runs on Windows 95 / 98 / ME / 2000 SP4+
Unicode false

!ifndef PATCH_OUTFILE
  !define PATCH_OUTFILE "tspatch110.exe"
!endif

Var CompanyName
Var ProductName
Var PreviousInstallFolder
Var TStoreVersionInstalled
Var TStoreVersionPatch
Var TSVerCompare
Var TSTOREVER
Var UNINSREG
Var hMutexCheck

!include /NONFATAL "ISUI.nsh"
!ifndef MUI_PAGE_WELCOME
  !include "MUI2.nsh"
!endif
!include "LogicLib.nsh"
!include "WinVer.nsh"
!include "Library.nsh"
!include "FileFunc.nsh"
!include "WordFunc.nsh"

Name "$CompanyName $ProductName"
OutFile "${PATCH_OUTFILE}"
BrandingText "$CompanyName"

ShowInstDetails show
ShowUninstDetails show

InstallDir "$PROGRAMFILES\$CompanyName\$ProductName"
InstallDirRegKey HKLM "Software\$CompanyName\$ProductName" "Installation Folder"

XPStyle on
RequestExecutionLevel admin
ManifestSupportedOS all

!getdllversion "..\Build\TStore.exe" Expv_

VIProductVersion "${Expv_1}.${Expv_2}.${Expv_3}.${Expv_4}"
VIAddVersionKey "ProductName" "$ProductName Patch"
VIAddVersionKey "Comments" "Updates StoreTrainer v1.0 / v1.1.x to ${Expv_1}.${Expv_2}.${Expv_3}.${Expv_4}"
VIAddVersionKey "CompanyName" "$CompanyName"
VIAddVersionKey "LegalTrademarks" "$ProductName is a creative imprint utility of $CompanyName"
VIAddVersionKey "LegalCopyright" "Copyright (C) 2018-2026 Michael P. Ragsdale (MIT License)"
VIAddVersionKey "FileDescription" "In-place update patch for RaggieSoft StoreTrainer"
VIAddVersionKey "FileVersion" "${Expv_1}.${Expv_2}.${Expv_3}.${Expv_4}"

  !define MUI_WELCOMEPAGE_TITLE "$CompanyName $ProductName Update Patch"
  !define MUI_WELCOMEPAGE_TEXT "Welcome to the update utility for $CompanyName $ProductName.$\r$\n$\r$\nThis patch will update your installed copy of $ProductName from version $TStoreVersionInstalled to version ${Expv_1}.${Expv_2}.${Expv_3}.${Expv_4}, adding .stf Explorer file associations, /l & /e switches, launch delay support, and RaggieSoft Launcher discovery.$\r$\n$\r$\n$_CLICK"
!insertmacro MUI_PAGE_WELCOME
  !define MUI_LICENSEPAGE_RADIOBUTTONS
  !define MUI_LICENSEPAGE_RADIOBUTTONS_TEXT_ACCEPT "I &Accept"
  !define MUI_LICENSEPAGE_RADIOBUTTONS_TEXT_DECLINE "I &Decline"
!insertmacro MUI_PAGE_LICENSE "License\LICENSE.rtf"
!insertmacro MUI_PAGE_INSTFILES
  !define MUI_FINISHPAGE_RUN "$INSTDIR\TStore.exe"
  !define MUI_FINISHPAGE_RUN_TEXT "&Launch $ProductName"
  !define MUI_FINISHPAGE_LINK "&Visit the $ProductName Website"
  !define MUI_FINISHPAGE_LINK_LOCATION "https://storetrainer.raggiesoft.com/"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

Section "$CompanyName $ProductName" TStoreBase
  SetShellVarContext all

  SetOutPath $INSTDIR
  SetOverwrite on
  DetailPrint "Updating $CompanyName $ProductName to ${Expv_1}.${Expv_2}.${Expv_3}.${Expv_4}..."
  File "..\Build\TStore.exe"
  File "..\Build\TStore.exe.manifest"
  File "..\README.txt"
  File "License\LICENSE.rtf"

  ${GetFileVersion} "$INSTDIR\TStore.exe" $TSTOREVER
  WriteINIStr "$INSTDIR\TStore.ini" "General" "Version" "$TSTOREVER"
  WriteINIStr "$INSTDIR\TStore.ini" "General" "Path" "$INSTDIR"

  CreateShortcut "$SMPROGRAMS\$ProductName.lnk" "$INSTDIR\TStore.exe" "" "$INSTDIR\TStore.exe" 0 SW_SHOWNORMAL "" "$ProductName stores the locations of your games and trainers"

  WriteRegStr HKLM "Software\$CompanyName\$ProductName" "Installation Folder" "$INSTDIR"
  WriteRegStr HKLM "Software\$CompanyName\$ProductName" "Product Version" "$TSTOREVER"
  WriteRegStr HKLM "Software\$CompanyName\$ProductName" "SMFolder" "$SMPROGRAMS"
  WriteRegStr HKLM "Software\$CompanyName\$ProductName" "Uninstall Key Location" "Software\Microsoft\Windows\CurrentVersion\Uninstall\$CompanyName $ProductName"

  ; Register '.stf' StoreFile association (double-click launches via '/l', right-click edits via '/e')
  WriteRegStr HKCR ".stf" "" "RaggieSoft.StoreFile"
  WriteRegStr HKCR "RaggieSoft.StoreFile" "" "RaggieSoft StoreTrainer Profile"
  WriteRegStr HKCR "RaggieSoft.StoreFile\DefaultIcon" "" "$\"$INSTDIR\TStore.exe$\",0"
  WriteRegStr HKCR "RaggieSoft.StoreFile\shell\open" "" "Launch Game & Trainer"
  WriteRegStr HKCR "RaggieSoft.StoreFile\shell\open\command" "" "$\"$INSTDIR\TStore.exe$\" /l $\"%1$\""
  WriteRegStr HKCR "RaggieSoft.StoreFile\shell\edit" "" "Edit in RaggieSoft StoreTrainer"
  WriteRegStr HKCR "RaggieSoft.StoreFile\shell\edit\command" "" "$\"$INSTDIR\TStore.exe$\" /e $\"%1$\""

  ; If upgrading from v1.0 where '$INSTDIR\Source' was installed locally, remove the retired
  ; v1.0 Source folder so old 1.0 files don't sit beside a patched 1.1 binary (full source now
  ; lives permanently on GitHub at https://github.com/raggiesoft/storetrainer).
  IfFileExists "$INSTDIR\Source\*.*" 0 +2
    RMDir /r "$INSTDIR\Source"
SectionEnd

Section "-Uninstall Registry Keys" Unins
  DetailPrint "Refreshing Uninstaller & RaggieSoft Launcher Metadata..."
  StrCpy $UNINSREG "Software\Microsoft\Windows\CurrentVersion\Uninstall\$CompanyName $ProductName"
  ${GetFileVersion} "$INSTDIR\TStore.exe" $R0

  ; Remove v1.0's historical 'Microsoft Windows' typo key if present
  DeleteRegKey HKLM "Software\Microsoft Windows\CurrentVersion\Uninstall\$CompanyName $ProductName"

  WriteRegStr HKLM $UNINSREG "DisplayName" "$CompanyName $ProductName"
  WriteRegStr HKLM $UNINSREG "DisplayVersion" $R0
  WriteRegStr HKLM $UNINSREG "Publisher" "$CompanyName"
  WriteRegStr HKLM $UNINSREG "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM $UNINSREG "DisplayIcon" "$INSTDIR\TStore.exe,0"
  WriteRegStr HKLM $UNINSREG "UninstallString" "$\"$INSTDIR\uninstall.exe$\""
  WriteRegStr HKLM $UNINSREG "URLInfoAbout" "https://storetrainer.raggiesoft.com/"
  WriteRegDWORD HKLM $UNINSREG "NoModify" 1
  WriteRegDWORD HKLM $UNINSREG "NoRepair" 1

  WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd

; Complete NSIS Uninstaller (replaces v1.0's uninstaller so future uninstalls clean up properly!)
Section "Uninstall"
  SetShellVarContext all
  ReadRegStr $INSTDIR HKLM "Software\$CompanyName\$ProductName" "Installation Folder"

  DetailPrint "Removing Registry Keys..."
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\$CompanyName $ProductName"
  DeleteRegKey HKLM "Software\Microsoft Windows\CurrentVersion\Uninstall\$CompanyName $ProductName"
  DeleteRegKey HKLM "Software\$CompanyName\$ProductName"
  DeleteRegKey /ifempty HKLM "Software\$CompanyName"
  DeleteRegKey HKCR "RaggieSoft.StoreFile"
  DeleteRegValue HKCR ".stf" ""

  DetailPrint "Removing $ProductName Application Files..."
  RMDir /r /REBOOTOK "$INSTDIR\Source"
  Delete "$INSTDIR\TStore.exe"
  Delete "$INSTDIR\TStore.exe.manifest"
  Delete "$INSTDIR\LICENSE.rtf"
  Delete "$INSTDIR\License.rtf"
  Delete "$INSTDIR\README.txt"
  Delete "$INSTDIR\TStore.ini"
  Delete "$INSTDIR\uninstall.exe"
  RMDir "$INSTDIR"
  RMDir "$PROGRAMFILES\$CompanyName"

  DetailPrint "Removing Start Menu Shortcut..."
  Delete "$SMPROGRAMS\$ProductName.lnk"
SectionEnd

Function .onInit
  StrCpy $CompanyName "RaggieSoft"
  StrCpy $ProductName "StoreTrainer"
  StrCpy $INSTDIR "$PROGRAMFILES\$CompanyName\$ProductName"

  ${If} ${IsNT}
    UserInfo::GetAccountType
    Pop $0
    ${If} $0 != "admin"
      MessageBox MB_ICONSTOP|MB_OK "Administrator rights are required to update $ProductName!" /SD IDOK
      SetErrorLevel 740
      Quit
    ${EndIf}

    ${If} ${IsWinNT4}
      ${If} ${AtMostServicePack} 2
        MessageBox MB_ICONSTOP|MB_OK "Service Pack 3 or higher is required on Windows NT 4.0." /SD IDOK
        SetErrorLevel 3
        Quit
      ${EndIf}
    ${EndIf}
  ${EndIf}

  ; Ensure TStore.exe is closed before patching
  System::Call 'kernel32::OpenMutexA(i 0x00100000, i 0, t "RaggieSoft_StoreTrainer_Mutex") i .r0'
  StrCpy $hMutexCheck $0
  ${If} $hMutexCheck != 0
    System::Call 'kernel32::CloseHandle(i $hMutexCheck)'
    MessageBox MB_ICONEXCLAMATION|MB_OK "Please close $CompanyName $ProductName before applying this update." /SD IDOK
    SetErrorLevel 8
    Quit
  ${EndIf}

  ; Locate existing StoreTrainer v1.0 or earlier v1.1.x installation
  ReadRegStr $PreviousInstallFolder HKLM "Software\$CompanyName\$ProductName" "Installation Folder"
  IfFileExists "$PreviousInstallFolder\TStore.exe" file_found file_not_found
  file_not_found:
    MessageBox MB_ICONSTOP|MB_OK "This update requires $CompanyName $ProductName v1.0 or v1.1 to be installed first!" /SD IDOK
    SetErrorLevel 7
    Quit
  file_found:
    StrCpy $INSTDIR "$PreviousInstallFolder"
    ${GetFileVersion} "$INSTDIR\TStore.exe" $TStoreVersionInstalled
    StrCpy $TStoreVersionPatch "${Expv_1}.${Expv_2}.${Expv_3}.${Expv_4}"
    ${VersionCompare} "$TStoreVersionInstalled" "$TStoreVersionPatch" $TSVerCompare

    ${If} $TSVerCompare == 0
      MessageBox MB_ICONINFORMATION|MB_OK "$CompanyName $ProductName $TStoreVersionInstalled is already up to date!" /SD IDOK
      SetErrorLevel 5
      Quit
    ${ElseIf} $TSVerCompare == 1
      MessageBox MB_ICONSTOP|MB_OK "A newer version of $ProductName ($TStoreVersionInstalled) is already installed!" /SD IDOK
      SetErrorLevel 6
      Quit
    ${EndIf}
FunctionEnd

Function un.onInit
  StrCpy $CompanyName "RaggieSoft"
  StrCpy $ProductName "StoreTrainer"

  ${If} ${IsNT}
    UserInfo::GetAccountType
    Pop $0
    ${If} $0 != "admin"
      MessageBox MB_ICONSTOP|MB_OK "Administrator rights are required to uninstall $ProductName!" /SD IDOK
      SetErrorLevel 740
      Quit
    ${EndIf}
  ${EndIf}
FunctionEnd
