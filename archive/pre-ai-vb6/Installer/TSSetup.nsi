;In NSIS, all variables are GLOBAL

;Variables for the Product Information
Var CompanyName
Var ProductName

;DCOM98
Var DCOM98INSTALL ;Do we need to install DCOM98? Yes or No
Var DCOM98RETURN ;What is the return code for DCOM98? (not necessary in our case, but NSIS will error out if there is no variable to hold the rerurn code, ALWAYS reboot after install)

;Windows Script Host
Var SCRRUNVER ;Version of WSH to check against (5.6)
Var SCRRUNDLL ;Version of WSH installed on machine
Var scr_exit_code ;Exit code from WSH installer

;Version of $ProductName installed
Var TSTOREVER

;Source Code installation folder. Typically $INSTDIR\Source
Var SRCCODE

;Unistall Registry Key Location
Var UNINSREG

;For comparing previous installations of $ProductName
Var IS_INSTALLED ;Yes or No: is $ProductName installed?
Var TSTORESETUP ;Variable to hold product version data

;InstallSpider UI (https://nsis.sourceforge.io/InstallSpiderUI)
;Drop in replacement for MUI2 to mimic Wizard95 installer interface
!include "ISUI.nsh"

;For using Logic operators
!include "LogicLib.nsh"

;Detect Windows Version
!include "WinVer.nsh"

;For installing my system libraries
!include "Library.nsh"

;For getting file functions
!include "FileFunc.nsh"

;For comparing versions
!include "WordFunc.nsh"

;Installer for $CompanyName $ProductName
Name "$CompanyName $ProductName"
OutFile "tssetup100.exe"
BrandingText "$CompanyName"

;Show file copy operations
ShowInstDetails show
ShowUninstDetails show

;Default install folder
InstallDir "$PROGRAMFILES\$CompanyName\$ProductName"

;Install Folder Registry Key
InstallDirRegKey HKLM "Software\$CompanyName\$ProductName" "Installation Folder"

;Subscribe to Common Controls 6.0 in Windows XP+
XPStyle on

;Request application privileges for Windows Vista and above
;See un.onInit and .onInit for older versions of WinNT Admin Rights checks
;un/.onInit will also handle Admin checks if UAC is turned off
RequestExecutionLevel admin

;What versions of Windows UAC dialogs to support
ManifestSupportedOS all

;Version this installer has
!getdllversion "..\Build\TStore.exe" Expv_

;Installation Version Information
VIProductVersion "${Expv_1}.${Expv_2}.${Expv_3}.${Expv_4}"
VIAddVersionKey "ProductName" "$ProductName"
VIAddVersionKey "Comments" ""
VIAddVersionKey "CompanyName" "$CompanyName"
VIAddVersionKey "LegalTrademarks" "$ProductName is a trademark of $CompanyName"
VIAddVersionKey "LegalCopyright" "Copyright (C) 2018-2020 $CompanyName"
VIAddVersionKey "FileDescription" "Stores the locations of games and trainers"
VIAddVersionKey "FileVersion" "${Expv_1}.${Expv_2}.${Expv_3}.${Expv_4}"


;Install Pages
!insertmacro MUI_PAGE_WELCOME
	;Set up our License page
	!define MUI_LICENSEPAGE_RADIOBUTTONS
	!define MUI_LICENSEPAGE_RADIOBUTTONS_TEXT_ACCEPT "I &Accept"
	!define MUI_LICENSEPAGE_RADIOBUTTONS_TEXT_DECLINE "I &Decline"
!insertmacro MUI_PAGE_LICENSE "License\LICENSE.rtf"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
	;Set up our Finish page
	!define MUI_FINISHPAGE_RUN $INSTDIR\TStore.exe
	!define MUI_FINISHPAGE_RUN_TEXT "&Launch $ProductName"
	!define MUI_FINISHPAGE_LINK "&Visit the $ProductName Website"
	!define MUI_FINISHPAGE_LINK_LOCATION "https://storetrainer.raggiesoft.com/"
!insertmacro MUI_PAGE_FINISH

;Uninstall Pages
!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

;Language Setting (yes, AFTER pages are loaded)
!insertmacro MUI_LANGUAGE "English"



;Microsoft Redistribution Files
;Only needed for older systems
;ABSOLUTELY POSITIVELY DO NOT ATTEMPT TO UNINSTALL!!!
Section "-Required System Components" SysComp
	;Most likely, these will only need to be installed on Windows 95/98
	;DO NOT ATTEMPT TO FORCIBLY REPLACE THESE FILES ON WINDOWS 2000 OR NEWER!!
	;For newer operating systems (2000, ME, and above), this will check with System File Protection before copying DLLs
	;This installer contains the 2012 re-release of the Visual Basic 6.0 DLLs
	
	;Start with the VB6 Runtime
	DetailPrint "Installing Microsoft Visual Basic 6.0 Runtime"
	!insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\msvbvm60.dll" "$SYSDIR\msvbvm60.dll" "$SYSDIR"
	!insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\oleaut32.dll" "$SYSDIR\oleaut32.dll" "$SYSDIR"
	!insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\olepro32.dll" "$SYSDIR\olepro32.dll" "$SYSDIR"
	!insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\comcat.dll" "$SYSDIR\comcat.dll" "$SYSDIR"
	!insertmacro InstallLib DLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\asycfilt.dll" "$SYSDIR\asycfilt.dll" "$SYSDIR"
	!insertmacro InstallLib TLB "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\stdole2.tlb"  "$SYSDIR\stdole2.tlb" "$SYSDIR"
	
	;Let's install the Windows Common Controls
	DetailPrint "Installing Windows Common Controls"
	!insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\comct232.ocx" "$SYSDIR\comct232.ocx" "$SYSDIR"
	!insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\comctl32.ocx" "$SYSDIR\comctl32.ocx" "$SYSDIR"
	
	;Let's install the Windows Common Dialog Control
	DetailPrint "Installing Windows Common Dialog Control"
	!insertmacro InstallLib REGDLL "${ALREADY_INSTALLED}" REBOOT_PROTECTED "Microsoft Redist\comdlg32.ocx" "$SYSDIR\comdlg32.ocx" "$SYSDIR"
	
	;Windows Script Host ActiveX Control
	;In a moment, we will also install the Windows Script Host Runtime
	DetailPrint "Installing Windows Script Host ActiveX Control"
	!insertmacro InstallLib REGDLL ${ALREADY_INSTALLED} REBOOT_PROTECTED "Microsoft Redist\wshom.ocx" "$SYSDIR\wshom.ocx" "$SYSDIR"

	;Let's get DCOM and the Windows Script Host going.  Only required for Windows 2000 or older
	SetOutPath $TEMP
	File "Microsoft Redist\DCOM98\DCOM98.exe"
	File "Microsoft Redist\WSH56\WScr9x.exe"
	File "Microsoft Redist\WSH56\WScrNT2k.exe"
	
	;Let's check for, and if necessary, install DCOM98 on this computer.
	;It is only needed on Windows 95/98
	${IfNot} ${IsNT}
		;Yes or no: is DCOM98 installed
		;If we need to drill down deeper, this returns a version string (but I don't need to)
		;A reboot is mandatory according to the Microsoft documentation
		;We are going to run in Silent mode, with no DCOM98 reboot (while telling NSIS to reboot at the end of EVERYTHING)
		ReadRegStr $DCOM98INSTALL HKCR "CLSID\{bdc67890-4fc0-11d0-a805-00aa006d2ea4}" "InstalledVersion"
		${If} $DCOM98INSTALL == ""
			DetailPrint "Installing DCOM98..."
			ExecWait "$\"$TEMP\DCOM98.exe$\" $\"/r:n /q:u$\"" $DCOM98RETURN
			SetRebootFlag true
		${EndIf}
	${EndIf}
	;Let's install Windows Script Host (already included with XP+)
	;First, let's get the file version of scrrun.dll (Scripting Host).  We have installers for version 5.6 of the runtime
	${GetFileVersion} "$SYSDIR\scrrun.dll" $SCRRUNDLL
	${VersionCompare} "5.6" $SCRRUNDLL $SCRRUNVER
	${If} $SCRRUNVER == "1"
		${If} ${IsNT}
			${If} ${AtMostWin2000}
				;Windows Script Host for Windows NT 4.0 and Windows 2000
				DetailPrint "Installing Windows Script Host 5.6 for Windows NT 4.0 and Windows 2000"
				ExecWait "$\"$TEMP\WScrNT2k.exe$\" $\"/q /r:n$\"" $scr_exit_code
				SetRebootFlag true
			${EndIf}
		${Else}
			;Windows Script Host for Windows 95/98
			DetailPrint "Installing Windows Script Host 5.6 for Windows 95 and 98"
			ExecWait "$\"$TEMP\Wscr9x.exe$\" $\"/q /r:n$\"" $scr_exit_code
			SetRebootFlag true
		${EndIf}
	${EndIf}
	

	
	;Finished installing System Components
SectionEnd
;$ProductName Install
Section "$CompanyName $ProductName" TStoreBase
	;We want Shortcuts to be for All Users
	SetShellVarContext all
	
	;Install Files
	SetOutPath $INSTDIR
	SetOverwrite ifnewer
	DetailPrint "Installing $ProductName"
	File "..\Build\TStore.exe"
	File "..\Build\TStore.exe.manifest"
	File "..\README.txt"
	File "License\LICENSE.rtf"
	
	;Get the Product Version
	${GetFileVersion} "$INSTDIR\TStore.exe" $TSTOREVER
	
	;Create Shortcut
	CreateShortcut "$SMPROGRAMS\$ProductName.lnk" "$INSTDIR\TStore.exe" "" "$INSTDIR\TStore.exe" 0 "SW_SHOWNORMAL" "" "$ProductName stores the locations of your games and trainers in an easy to use format"
	
	;Create $ProductName Registry entries
	WriteRegStr HKLM "Software\$CompanyName\$ProductName" "Installation Folder" "$INSTDIR"
	WriteRegStr HKLM "Software\$CompanyName\$ProductName" "Product Version" "$TSTOREVER"
	WriteRegStr HKLM "Software\$CompanyName\$ProductName" "SMFolder" "$SMPROGRAMS"
	WriteRegStr HKLM "Software\$CompanyName\$ProductName" "Uninstall Key Location" "Software\Microsoft\Windows\CurrentVersion\Uninstall\$CompanyName $ProductName"
	
	;Create master TSTORE.INI file (meant to be read-only)
	WriteINIStr "$INSTDIR\TStore.ini" "General" "Version" "$TSTOREVER"
	WriteINIStr "$INSTDIR\TStore.ini" "General" "Path" "$INSTDIR"
	
SectionEnd
Section "Source Code" Src
	;Install the Source Code
	DetailPrint "Installing Source Code"
	StrCpy $SRCCODE "$INSTDIR\Source"
	CreateDirectory "$SRCCODE"
	SetOutPath "$SRCCODE"
	
	;$ProductName Source Code
	File "..\DetectOSBits.bas"
	File "..\frmMain.frm"
	File "..\INIFile.bas"
	File "..\OSVersionInfo.bas"
	File "..\README.txt"
	File "..\Registry.bas"
	File "..\SFile.bas"
	File "..\SpecialFolder.bas"
	File "..\TStore.bas"
	File "..\TStore.PDM"
	File "..\TStore.vbp"
	File "..\TStore.vbw"
	File "..\VersionCompare.bas"
	File "..\wHwnd.bas"
	CreateDirectory "$SRCCODE\Installer"
	CreateDirectory "$SRCCODE\Build"
	SetOutPath "$SRCCODE\Installer"
	File "TSSetup.nsi"
	File "TSSetup-Patch.nsi"
	
	;Let's recreate the Microsoft Redist folders that TSSetup.nsi expects
	CreateDirectory "$SRCCODE\Installer\Microsoft Redist"
	SetOutPath "$SRCCODE\Installer\Microsoft Redist"
	
	;Copy the existing files so that they are in the locations TSSetup.nsi expects
	;The numbers at the end are the size in KB so the insaller can correctly do disk costing
	;NOTE: The Visual Basic 6.0 runtime files are from the 2012 Security Update
	;The InstallLib macro above correctly handles file updates, even on modern versions of Windows
	;DO NOT ATTEMPT TO REGISTER THESE COPIES OF SYSTEM FILES - YOU WILL BREAK WINDOWS
	CopyFiles "$SYSDIR\asycfilt.dll" "$SRCCODE\Installer\Microsoft Redist" 145
	CopyFiles "$SYSDIR\comcat.dll" "$SRCCODE\Installer\Microsoft Redist" 22
	CopyFiles "$SYSDIR\comct232.ocx" "$SRCCODE\Installer\Microsoft Redist" 167
	CopyFiles "$SYSDIR\comctl32.ocx" "$SRCCODE\Installer\Microsoft Redist" 604
	CopyFiles "$SYSDIR\comdlg32.ocx" "$SRCCODE\Installer\Microsoft Redist" 153
	CopyFiles "$SYSDIR\msvbvm60.dll" "$SRCCODE\Installer\Microsoft Redist" 1354
	CopyFiles "$SYSDIR\oleaut32.dll" "$SRCCODE\Installer\Microsoft Redist" 585
	CopyFiles "$SYSDIR\olepro32.dll" "$SRCCODE\Installer\Microsoft Redist" 161
	CopyFiles "$SYSDIR\stdole2.tlb" "$SRCCODE\Installer\Microsoft Redist" 18
	CopyFiles "$SYSDIR\wshom.ocx" "$SRCCODE\Installer\Microsoft Redist" 101
	
	CreateDirectory "$SRCCODE\Installer\Microsoft Redist\DCOM98"
	SetOutPath "$SRCCODE\Installer\Microsoft Redist\DCOM98"
	CopyFiles "$TEMP\DCOM98.exe" "$SRCCODE\Installer\Microsoft Redist\DCOM98" 1194
	
	CreateDirectory "$SRCCODE\Installer\Microsoft Redist\WSH56"
	SetOutPath "$SRCCODE\Installer\Microsoft Redist\WSH56"
	CopyFiles "$TEMP\WScr9x.exe" "$SRCCODE\Installer\Microsoft Redist\WSH56" 716
	CopyFiles "$TEMP\WScrNT2k.exe" "$SRCCODE\Installer\Microsoft Redist\WSH56" 716
SectionEnd
Section "-Unistall Registry Keys" Unins
	;Create Uninstall Registry Entries
	DetailPrint "Creating Uninstaller"
	StrCpy $UNINSREG "Software\Microsoft\Windows\CurrentVersion\Uninstall\$CompanyName $ProductName"
	
	;Get $ProductName file version for the Uninstall Registry key
	${GetFileVersion} "$INSTDIR\TStore.exe" $R0
	
	;Write the keys to the Registry
	WriteRegStr HKLM $UNINSREG "DisplayName" "$CompanyName $ProductName"
	WriteRegStr HKLM $UNINSREG "DisplayVersion" $R0
	WriteRegStr HKLM $UNINSREG "UninstallString" "$INSTDIR\uninstall.exe"
	WriteRegStr HKLM $UNINSREG "Publisher" "$CompanyName"
	
	;Write our Uninstaller
	WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd
Section "-Cleanup" Cleanup
	;Let's remove the redist installers
	DetailPrint "Cleaning up"
	Delete "$TEMP\DCOM98.exe"
	Delete "$TEMP\Wscr9x.exe"
	Delete "$TEMP\WScrNT2k.exe"
SectionEnd

;Section Descriptions
LangString DESC_TStoreBase ${LANG_ENGLISH} "$ProductName stores the locations of games and trainers"
LangString DESC_Src ${LANG_ENGLISH} "Source Code for $ProductName (Visual Basic 6)"
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${TStoreBase} $(DESC_TStoreBase)
	!insertmacro MUI_DESCRIPTION_TEXT ${Src} $(DESC_Src)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;This runs when the program is uninstalling
Section "Uninstall"
	;We want Shortcuts to be for All Users
	SetShellVarContext all
	
	;Grab the proper installation folder (in case the uninstaller is in the wrong place)
	ReadRegStr $INSTDIR HKLM "Software\$CompanyName\$ProductName" "Installation Folder"
	
	;Remove our Registry keys
	DetailPrint "Deleting Registry Keys"
	DeleteRegKey HKLM "Software\Microsoft Windows\CurrentVersion\Uninstall\$CompanyName $ProductName"
	DeleteRegKey HKLM "Software\$CompanyName\$ProductName"
	DeleteRegKey /ifempty HKLM "Software\$CompanyName"
	
	;Remove $ProductName's executable
	DetailPrint "Uninstalling $ProductName"
	RMDir /r /REBOOTOK "$INSTDIR\Source" ;Recursively search the folder to clear out any extra files
	Delete "$INSTDIR\TStore.exe"
	Delete "$INSTDIR\TStore.exe.manifest"
	Delete "$INSTDIR\License.rtf"
	Delete "$INSTDIR\README.txt"
	Delete "$INSTDIR\TStore.ini"
	Delete "$INSTDIR\uninstall.exe"
	RMDir "$INSTDIR" ;DO NOT do a /r recursive search (in case, for example, someone installed to C:\Program Files this would really mess up the users computer)
	RMDir $PROGRAMFILES\$CompanyName ;AGAIN, do NOT use /r (this deletes it ONLY IF EMPTY)
	
	DetailPrint "Deleting Shortcuts"
	Delete "$SMPROGRAMS\$ProductName.lnk"
	
	;We are DONE with uninstall
	;As a reminder, DO NOT TOUCH any of the Microsoft DLLs that get installed
	;Also, $ProductName's per-user files do not get uninstalled (it's just not possible to remove files for users other than your own in the main uninstaller, however it is possible to release a utility that does PER-USER cleaning)
	
SectionEnd

;We need to do some checks when the installer and uninstaller are started.

;Tests for Admin rights NT4/2000/XP
;Also works if somehow UAC is disabled or broken on Vista+
;Also, on Windows NT 4.0, the Visual Basic runtime requires Service Pack 3 or later installed
Function .onInit
	;Company and Product
	StrCpy $CompanyName "RaggieSoft"
	StrCpy $ProductName "StoreTrainer"
	
	;Redefine the install folder
	StrCpy $INSTDIR "$PROGRAMFILES\$CompanyName\$ProductName"
	
	;Check for NT Admin Rights
	UserInfo::GetAccountType
	pop $0
	${If} $0 != "admin" ;Require admin rights on NT4+
		MessageBox MB_ICONSTOP|MB_OK "Administrator rights required to install $ProductName!" /SD IDOK
		SetErrorLevel 740 ;ERROR_ELEVATION_REQUIRED
		Quit
	${EndIf}

	;Check for NT 4.0 SP3 or higher
	;Otherwise, $ProductName *SHOULD* be able to run on any other 32bit or 64bit OS
	;That is Windows 95 or later.  Sorry Windows 3.1
	${If} ${IsNT}
		${If} ${IsWinNT4}
			${If} ${AtMostServicePack} 2
				MessageBox MB_ICONSTOP|MB_OK "Service Pack 3 or higher is required for Windows NT 4.0" /SD IDOK
				SetErrorLevel 3 ;3 because Service Pack 3 (pun intended)
				Quit
			${EndIf}
		${EndIf}
	${EndIf}
	
	;Detect previous version
	ReadRegStr  $IS_INSTALLED HKLM "Software\$CompanyName\$ProductName" "Installation Folder"
	IfFileExists "$IS_INSTALLED\uninstall.exe" FileFound EndFileExistsTest
	FileFound:
		;Let's check file versions.
		${GetFileVersion} "$INSTDIR\TStore.exe" $R0
		${GetFileVersion} "$EXEPATH" $R1
		${VersionCompare} $R0 $R1 $TSTORESETUP
		
		;This installer is the same as what is already installed
		${If} $TSTORESETUP == 0
			Exec "$IS_INSTALLED\uninstall.exe"
			SetErrorLevel 4
			Quit
		${EndIf}
		
		;This installer has an older version of $ProductName
		${If} $TSSETUP == 1
			MessageBox MB_ICONSTOP|MB_OK "This installer contains an older version of $ProductName.$\r$\nSetup will now close." /SD IDOK
			SetErrorLevel 5
			Quit
		${EndIf}
	EndFileExistsTest:

		
FunctionEnd
Function un.onInit
	;Company and Product
	StrCpy $CompanyName "RaggieSoft"
	StrCpy $ProductName "StoreTrainer"
	
	UserInfo::GetAccountType
	pop $0
	${If} $0 != "admin" ;Require admin rights on NT4+
		MessageBox MB_ICONSTOP|MB_OK "Administrator rights required to uninstall $ProductName!" /SD IDOK
		SetErrorLevel 740 ;ERROR_ELEVATION_REQUIRED
		Quit
	${EndIf}
FunctionEnd