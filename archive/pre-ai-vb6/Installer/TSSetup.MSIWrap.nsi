; Variables for the Product Information
Var CompanyName
Var ProductName

; Variables for the Windows Installer
Var MSICODE
Var MSIReturn

; InstallSpider UI
!include "ISUI.nsh"

; NSIS Dialogs
!include "nsDialogs.nsh"

; For using Logic operators
!include "LogicLib.nsh"

; Detect Windows Version
!include "WinVer.nsh"

; For installing my system libraries
!include "Library.nsh"

; For getting file functions
!include "FileFunc.nsh"

; For comparing versions
!include "WordFunc.nsh"

; Installer for $CompanyName $ProductName
Name "$CompanyName $ProductName"
OutFile "tssetup103.exe"
BrandingText "$CompanyName"

; Show file copy operations
ShowInstDetails show
ShowUninstDetails show

; Default install folder
InstallDir "$PROGRAMFILES\$CompanyName\$ProductName"

; Install Folder Registry Key
InstallDirRegKey HKLM "Software\$CompanyName\$ProductName" "Installation Folder"

; Subscribe to Common Controls 6.0 in Windows XP+
XPStyle on

; Request application privileges for Windows Vista and above
; See un.onInit and .onInit for older versions of WinNT Admin Rights checks
; un/.onInit will also handle Admin checks if UAC is turned off
RequestExecutionLevel admin

; What versions of Windows UAC dialogs to support
ManifestSupportedOS all

; Version this installer has
!getdllversion "..\Build\TStore.exe" Expv_

; Installation Version Information
VIProductVersion "${Expv_1}.${Expv_2}.${Expv_3}.${Expv_4}"
VIAddVersionKey "ProductName" "$ProductName"
VIAddVersionKey "Comments" ""
VIAddVersionKey "CompanyName" "$CompanyName"
VIAddVersionKey "LegalTrademarks" "$ProductName is a trademark of $CompanyName"
VIAddVersionKey "LegalCopyright" "Copyright (C) 2018-2020 $CompanyName"
VIAddVersionKey "FileDescription" "Stores the locations of games and trainers"
VIAddVersionKey "FileVersion" "${Expv_1}.${Expv_2}.${Expv_3}.${Expv_4}"

; Uninstall Dialog: Offer Repair or Remove
; Repair can offer a full reinstall or just a repair
Var RepairType ; Expected: REINSTALL /faumsv, REPAIR /fpums, or REMOVE /x
Var MSISwitch ; One place to hold our MSI command line switches


; Install Pages
!insertmacro MUI_PAGE_WELCOME
	; Set up our License page
	!define MUI_LICENSEPAGE_RADIOBUTTONS
	!define MUI_LICENSEPAGE_RADIOBUTTONS_TEXT_ACCEPT "I &Accept"
	!define MUI_LICENSEPAGE_RADIOBUTTONS_TEXT_DECLINE "I &Decline"
!insertmacro MUI_PAGE_LICENSE "License\LICENSE.rtf"

!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
	; Set up our Finish page
	!define MUI_FINISHPAGE_RUN $INSTDIR\TStore.exe
	!define MUI_FINISHPAGE_RUN_TEXT "&Launch $ProductName"
	!define MUI_FINISHPAGE_LINK "&Visit the $ProductName Website"
	!define MUI_FINISHPAGE_LINK_LOCATION "https://storetrainer.raggiesoft.com/"
!insertmacro MUI_PAGE_FINISH

; Uninstall Pages
!insertmacro MUI_UNPAGE_WELCOME
Page custom pgRepairTypeCreate pgRepairTypeLeave
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

; Language Setting (yes, AFTER pages are loaded)
!insertmacro MUI_LANGUAGE "English"

; Microsoft Redist are now available inside the MSI file


; $ProductName Install
Section "$CompanyName $ProductName" TStoreBase
	; We want Shortcuts to be for All Users
	SetShellVarContext all
	
	; Extract our MSI to %ProgramData%
	CreateDirectory $LOCALAPPDATA\$CompanyName\Installers\$ProductName
	SetOutPath $LOCALAPPDATA\$CompanyName\Installers\$ProductName
	DetailPrint "Please wait while Windows configures $ProductName"
	File "MSI\TStore.msi"
	
	; Install $ProductName on the users computer
	ExecWait "'$SYSDIR\msiexec.exe' /i '$LOCALAPPDATA\$CompanyName\Installers\$ProductName\TStore.msi' /norestart /qb-! SETUPEXE=present INSTALLDIR='$INSTDIR'" $MSIReturn
	
	; Windows Installer requested a reboot
	${If} $MSIReturn == 1641
		SetRebootFlag true
	${EndIf}
	${If} $MSIReturn == 3010
		SetRebootFlag true
	${EndIf}
	
	; Create master TSTORE.INI file (meant to be read-only)
	WriteINIStr "$INSTDIR\TStore.ini" "General" "Version" "$TSTOREVER"
	WriteINIStr "$INSTDIR\TStore.ini" "General" "Path" "$INSTDIR"
	
SectionEnd



; This runs when the program is uninstalling
Section "Uninstall"
	; We want Shortcuts to be for All Users
	SetShellVarContext all
	
	; Grab the proper installation folder (in case the uninstaller is in the wrong place)
	ReadRegStr $INSTDIR HKLM "Software\$CompanyName\$ProductName" "Installation Folder"
	
	; Grab our MSI Code
	ReadRegStr $MSICODE HKLM "Software\$CompanyName\$ProductName" "MSI Code"
	
	; Did someone run msiexec /x [ProductCode]? Error 1001
	${If} $MSICODE == ""
		MessageBox MB_ICONSTOP|MB_OK "$ProductName is not installed on this system!" /SD IDOK
		SetErrorLevel 1001
		Abort
	${EndIf}
	
	; Uninstall $ProductName
	DetailPrint "Please wait while Windows configures $ProductName"
	ExecWait "'$SYSDIR\msiexec.exe' /x $MSICODE /norestart /qb-! SETUPEXE=present" $MSIReturn
	
	; Windows Installer requested a reboot
	${If} $MSIReturn == 1641
		SetRebootFlag true
	${EndIf}
	${If} $MSIReturn == 3010
		SetRebootFlag true
	${EndIf}
	
	; Remove the MSI File
	Delete "$LOCALAPPDATA\$CompanyName\Installers\$ProductName\TStore.msi"
	RMDir /r "$LOCALAPPDATA\$CompanyName\Installers\$ProductName\MSP" ;Any patches that come out
	RMDir "$LOCALAPPDATA\$CompanyName\Installers\$ProductName"
	RMDir "$LOCALAPPDATA\$CompanyName\Installers\"
	RMDir "$LOCALAPPDATA\$CompanyName"
	
SectionEnd

; Repair Page
Function pgRepairTypeCreate
	; Repair Type Header
	!insertmacro 

FunctionEnd

; We need to do some checks when the installer and uninstaller are started.

; Tests for Admin rights NT4/2000/XP
; Also works if somehow UAC is disabled or broken on Vista+
; Also, on Windows NT 4.0, the Visual Basic runtime requires Service Pack 3 or later installed
Function .onInit
	; Company and Product
	StrCpy $CompanyName "RaggieSoft"
	StrCpy $ProductName "StoreTrainer"
	
	; Redefine the install folder
	StrCpy $INSTDIR "$PROGRAMFILES\$CompanyName\$ProductName"
	
	; Check for NT Admin Rights
	UserInfo::GetAccountType
	pop $0
	${If} $0 != "admin" ;Require admin rights on NT4+
		MessageBox MB_ICONSTOP|MB_OK "Administrator rights required to install $ProductName!" /SD IDOK
		SetErrorLevel 740 ;ERROR_ELEVATION_REQUIRED
		Quit
	${EndIf}

	; Check for NT 4.0 SP6 or higher (That is a Windows Installer requirement)
	; Otherwise, $ProductName *SHOULD* be able to run on any other 32bit or 64bit OS
	; That is Windows 95 or later.  Sorry Windows 3.1
	${If} ${IsNT}
		${If} ${IsWinNT4}
			${If} ${AtMostServicePack} 5
				MessageBox MB_ICONSTOP|MB_OK "Service Pack 6 or higher is required for Windows NT 4.0" /SD IDOK
				SetErrorLevel 6 ;6 because Service Pack 6 (pun intended)
				Quit
			${EndIf}
		${EndIf}
	${EndIf}
	
	; Detect previous version
	Var /GLOBAL IS_INSTALLED
	ReadRegStr  $IS_INSTALLED HKLM "Software\$CompanyName\$ProductName" "Installation Folder"
	IfFileExists "$IS_INSTALLED\uninstall.exe" FileFound EndFileExistsTest
	FileFound:
		; Let's check file versions.
		Var /GLOBAL TSTORESETUP
		${GetFileVersion} "$INSTDIR\TStore.exe" $R0
		${GetFileVersion} "$EXEPATH" $R1
		${VersionCompare} $R0 $R1 $TSTORESETUP
		
		; This installer is the same as what is already installed
		${If} $TSTORESETUP == 0
			Exec "$IS_INSTALLED\uninstall.exe"
			SetErrorLevel 4
			Quit
		${EndIf}
		
		; This installer has an older version of $ProductName
		${If} $TSSETUP == 1
			MessageBox MB_ICONSTOP|MB_OK "This installer contains an older version of $ProductName.$\r$\nSetup will now close." /SD IDOK
			SetErrorLevel 5
			Quit
		${EndIf}
	EndFileExistsTest:

		
FunctionEnd
Function un.onInit
	; Company and Product
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