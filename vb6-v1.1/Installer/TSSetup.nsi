; ==============================================================================
; FILE: TSSetup.nsi
; APPLICATION: RaggieSoft StoreTrainer v1.1 (Classic Visual Basic 6.0 Edition)
; COMPILER: Nullsoft Scriptable Install System (NSIS 2.x or NSIS 3.x in ANSI Mode)
; TARGET OS: Windows 95, 98, ME, NT 4.0 SP3+, 2000 SP4, XP, Vista, 7, 8.1, 10, 11
; LICENSE: MIT License
;
; PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
;   What is this file?
;     This '.nsi' script compiles 'tssetup110.exe' — the full retro installer for
;     classic 32-bit Visual Basic 6.0 StoreTrainer v1.1!
;
;   Why stay on NSIS with 'Unicode false' for v1.1 while v2.0 uses Inno Setup 7?
;     - Inno Setup 7 requires Windows 7 SP1 / Windows 10+ and 64-bit CPUs.
;     - By declaring 'Unicode false' right at the top of this NSIS script, NSIS 3
;       compiles an x86 ANSI installer stub using '*A' Win32 APIs that genuinely
;       runs on vintage Windows 95, Windows 98, and Windows 2000 retro gaming PCs!
;
;   What did v1.1 fix and add compared to v1.0's TSSetup.nsi?
;     1. Explicit 'Unicode false' so NSIS 3 preserves Windows 95 compatibility.
;     2. Fixed v1.0's two bugs: '$TSSETUP' -> '$TSTORESETUP' version-compare variable
;        typo in '.onInit', and 'Software\Microsoft Windows\...' missing backslash
;        in 'Section "Uninstall"' (while also cleaning up any leftover v1.0 typo key!).
;     3. Checks our named Win32 Mutex ('RaggieSoft_StoreTrainer_Mutex') before
;        overwriting 'TStore.exe'.
;     4. Registers '.stf' file associations ('open' -> '/l "%1"', 'edit' -> '/e "%1"').
;     5. Writes complete Uninstall metadata ('InstallLocation', 'DisplayIcon',
;        'URLInfoAbout') so RaggieSoft Launcher automatically discovers StoreTrainer v1.1!
; ==============================================================================

; Force 32-bit ANSI installer stub in NSIS 3.x so Windows 95 / 98 / ME are supported!
Unicode false

!ifndef SETUP_OUTFILE
  !define SETUP_OUTFILE "tssetup110.exe"
!endif

Var CompanyName
Var ProductName
Var DCOM98INSTALL
Var DCOM98RETURN
Var SCRRUNVER
Var SCRRUNDLL
Var scr_exit_code
Var TSTOREVER
Var UNINSREG
Var IS_INSTALLED
Var TSTORESETUP
Var hMutexCheck

; Use InstallSpider UI if installed in NSIS\Include, otherwise fall back cleanly to MUI2
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
OutFile "${SETUP_OUTFILE}"
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
VIAddVersionKey "ProductName" "$ProductName"
VIAddVersionKey "Comments" "Classic Retro Edition (Windows 95 through Windows 11)"
VIAddVersionKey "CompanyName" "$CompanyName"
VIAddVersionKey "LegalTrademarks" "$ProductName is a creative imprint utility of $CompanyName"
VIAddVersionKey "LegalCopyright" "Copyright (C) 2018-2026 Michael P. Ragsdale (MIT License)"
VIAddVersionKey "FileDescription" "Stores the locations of games and trainers"
VIAddVersionKey "FileVersion" "${Expv_1}.${Expv_2}.${Expv_3}.${Expv_4}"

; Wizard Pages
!insertmacro MUI_PAGE_WELCOME
  !define MUI_LICENSEPAGE_RADIOBUTTONS
  !define MUI_LICENSEPAGE_RADIOBUTTONS_TEXT_ACCEPT "I &Accept"
  !define MUI_LICENSEPAGE_RADIOBUTTONS_TEXT_DECLINE "I &Decline"
!insertmacro MUI_PAGE_LICENSE "License\LICENSE.rtf"
!insertmacro MUI_PAGE_DIRECTORY
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

; ==============================================================================
; SECTION 1: Legacy VB6 / DCOM98 / WSH 5.6 Runtime Libraries (NEVER UNINSTALL!)
; ==============================================================================
Section "-Required System Components" SysComp
  DetailPrint "Checking Microsoft Visual Basic 6.0 Runtime..."
  !insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\msvbvm60.dll" "$SYSDIR\msvbvm60.dll" "$SYSDIR"
  !insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\oleaut32.dll" "$SYSDIR\oleaut32.dll" "$SYSDIR"
  !insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\olepro32.dll" "$SYSDIR\olepro32.dll" "$SYSDIR"
  !insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\comcat.dll" "$SYSDIR\comcat.dll" "$SYSDIR"
  !insertmacro InstallLib DLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\asycfilt.dll" "$SYSDIR\asycfilt.dll" "$SYSDIR"
  !insertmacro InstallLib TLB "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\stdole2.tlb" "$SYSDIR\stdole2.tlb" "$SYSDIR"

  DetailPrint "Checking Windows Common Controls & Dialogs..."
  !insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\comct232.ocx" "$SYSDIR\comct232.ocx" "$SYSDIR"
  !insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\comctl32.ocx" "$SYSDIR\comctl32.ocx" "$SYSDIR"
  !insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\comdlg32.ocx" "$SYSDIR\comdlg32.ocx" "$SYSDIR"
  !insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\wshom.ocx" "$SYSDIR\wshom.ocx" "$SYSDIR"

  SetOutPath $TEMP
  File "Microsoft Redist\DCOM98\DCOM98.exe"
  File "Microsoft Redist\WSH56\WScr9x.exe"
  File "Microsoft Redist\WSH56\WScrNT2k.exe"

  ${IfNot} ${IsNT}
    ReadRegStr $DCOM98INSTALL HKCR "CLSID\{bdc67890-4fc0-11d0-a805-00aa006d2ea4}" "InstalledVersion"
    ${If} $DCOM98INSTALL == ""
      DetailPrint "Installing DCOM98 for Windows 95/98..."
      ExecWait "$\"$TEMP\DCOM98.exe$\" /r:n /q:u" $DCOM98RETURN
      SetRebootFlag true
    ${EndIf}
  ${EndIf}

  ${GetFileVersion} "$SYSDIR\scrrun.dll" $SCRRUNDLL
  ${VersionCompare} "5.6" $SCRRUNDLL $SCRRUNVER
  ${If} $SCRRUNVER == "1"
    ${If} ${IsNT}
      ${If} ${AtMostWin2000}
        DetailPrint "Installing Windows Script Host 5.6 for Windows NT 4.0 / 2000..."
        ExecWait "$\"$TEMP\WScrNT2k.exe$\" /q /r:n" $scr_exit_code
        SetRebootFlag true
      ${EndIf}
    ${Else}
      DetailPrint "Installing Windows Script Host 5.6 for Windows 95 / 98..."
      ExecWait "$\"$TEMP\WScr9x.exe$\" /q /r:n" $scr_exit_code
      SetRebootFlag true
    ${EndIf}
  ${EndIf}
SectionEnd

; ==============================================================================
; SECTION 2: Core StoreTrainer v1.1 Binary, File Associations & Launcher Keys
; ==============================================================================
Section "$CompanyName $ProductName" TStoreBase
  SetShellVarContext all

  SetOutPath $INSTDIR
  SetOverwrite ifnewer
  DetailPrint "Installing $CompanyName $ProductName..."
  File "..\Build\TStore.exe"
  File "..\Build\TStore.exe.manifest"
  File "..\README.txt"
  File "License\LICENSE.rtf"

  ${GetFileVersion} "$INSTDIR\TStore.exe" $TSTOREVER

  CreateShortcut "$SMPROGRAMS\$ProductName.lnk" "$INSTDIR\TStore.exe" "" "$INSTDIR\TStore.exe" 0 SW_SHOWNORMAL "" "$ProductName stores the locations of your games and trainers"

  ; Application state Registry keys (also read by TSSetup-Patch.nsi and StoreTrainer 2.0's upgrader)
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

  WriteINIStr "$INSTDIR\TStore.ini" "General" "Version" "$TSTOREVER"
  WriteINIStr "$INSTDIR\TStore.ini" "General" "Path" "$INSTDIR"
SectionEnd

; ==============================================================================
; SECTION 3: Windows Uninstall & RaggieSoft Launcher Discovery Keys
;   (Note: Bundled VB6 source code was retired in v1.1 since the complete
;    multi-generation source archive is published at github.com/raggiesoft/storetrainer.)
; ==============================================================================
Section "-Uninstall Registry Keys" Unins
  DetailPrint "Registering Uninstaller & RaggieSoft Launcher Metadata..."
  StrCpy $UNINSREG "Software\Microsoft\Windows\CurrentVersion\Uninstall\$CompanyName $ProductName"

  ${GetFileVersion} "$INSTDIR\TStore.exe" $R0

  ; Clean up v1.0's historical 'Microsoft Windows' typo key if present from an older install
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

Section "-Cleanup" Cleanup
  Delete "$TEMP\DCOM98.exe"
  Delete "$TEMP\WScr9x.exe"
  Delete "$TEMP\WScrNT2k.exe"
SectionEnd

; ==============================================================================
; UNINSTALL SECTION (Leaves shared VB6 system DLLs & user My StoreFiles safe!)
; ==============================================================================
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

  ${If} $INSTDIR == ""
    StrCpy $INSTDIR "$PROGRAMFILES\$CompanyName\$ProductName"
  ${EndIf}

  ${If} ${IsNT}
    UserInfo::GetAccountType
    Pop $0
    ${If} $0 != "admin"
      MessageBox MB_ICONSTOP|MB_OK "Administrator rights are required to install $ProductName!" /SD IDOK
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

  ; Check if TStore.exe (or StoreTrainer 2.0) is currently open via our shared Win32 Mutex
  System::Call 'kernel32::OpenMutexA(i 0x00100000, i 0, t "RaggieSoft_StoreTrainer_Mutex") i .r0'
  StrCpy $hMutexCheck $0
  ${If} $hMutexCheck != 0
    System::Call 'kernel32::CloseHandle(i $hMutexCheck)'
    MessageBox MB_ICONEXCLAMATION|MB_OK "Please close $CompanyName $ProductName before running Setup." /SD IDOK
    SetErrorLevel 6
    Quit
  ${EndIf}

  ; Check existing installed version ( supports clean v1.0 -> v1.1 and future v1.1.x upgrades! )
  ReadRegStr $IS_INSTALLED HKLM "Software\$CompanyName\$ProductName" "Installation Folder"
  IfFileExists "$IS_INSTALLED\TStore.exe" FileFound EndFileExistsTest
  FileFound:
    StrCpy $INSTDIR "$IS_INSTALLED"
    ${GetFileVersion} "$INSTDIR\TStore.exe" $R0
    ${GetFileVersion} "$EXEPATH" $R1
    ${VersionCompare} $R0 $R1 $TSTORESETUP

    ${If} $TSTORESETUP == 0
      Exec "$IS_INSTALLED\uninstall.exe"
      SetErrorLevel 4
      Quit
    ${ElseIf} $TSTORESETUP == 1
      MessageBox MB_ICONSTOP|MB_OK "A newer version of $ProductName ($R0) is already installed.$\r$\nSetup will now close." /SD IDOK
      SetErrorLevel 5
      Quit
    ${EndIf}
  EndFileExistsTest:
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
