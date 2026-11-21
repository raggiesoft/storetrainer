; Variables for the Product Information
Var CompanyName
Var ProductName

; InstallSpider UI
!include "ISUI.nsh"

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

; Installer for RaggieSoft StoreTrainer
Name "$CompanyName $ProductName"
OutFile "tspatch104.exe"
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

Var PreviousInstallFolder
Var TStoreVersionInstalled
Var TStoreVersionPatch
Var TSVerCompare

; Install Pages
	;Redefine the text on the Welcome page to let everyone know that this is a Patch installer
	!define MUI_WELCOMEPAGE_TITLE "$CompanyName $ProductName Patcher"
	!define MUI_WELCOMEPAGE_TEXT "Welcome to the patch utility for $CompanyName $ProductName.  This installer will update your copy of $ProductName from version $TStoreVersionInstalled to version ${Expv_1}.${Expv_2}.${Expv_3}.${Expv_4}.$\r$\n$\r$\nIt is recommended that you close all other applications before starting Setup. This will make it possible to update relevant system files without having to reboot your computer$\r$\n$\r$\n$_CLICK"
!insertmacro MUI_PAGE_WELCOME
	; Set up our License page
	!define MUI_LICENSEPAGE_RADIOBUTTONS
	!define MUI_LICENSEPAGE_RADIOBUTTONS_TEXT_ACCEPT "I &Accept"
	!define MUI_LICENSEPAGE_RADIOBUTTONS_TEXT_DECLINE "I &Decline"
!insertmacro MUI_PAGE_LICENSE "License\LICENSE.rtf"
;!insertmacro MUI_PAGE_COMPONENTS
;!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
	; Set up our Finish page
	!define MUI_FINISHPAGE_RUN $INSTDIR\TStore.exe
	!define MUI_FINISHPAGE_RUN_TEXT "&Launch $ProductName"
	!define MUI_FINISHPAGE_LINK "&Visit the $ProductName Website"
	!define MUI_FINISHPAGE_LINK_LOCATION "https://storetrainer.raggiesoft.com/"
!insertmacro MUI_PAGE_FINISH

; Uninstall Pages
!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

; Language Setting (yes, AFTER pages are loaded)
!insertmacro MUI_LANGUAGE "English"

; $ProductName Install
Section "$CompanyName $ProductName" TStoreBase
	; Install Files
	SetOutPath $INSTDIR
	SetOverwrite ifnewer
	DetailPrint "Installing $ProductName"
	File "..\Build\TStore.exe"
	File "..\Build\TStore.exe.manifest"
	File "..\README.txt"
	File "License\LICENSE.rtf"
	
	; Create master TSTORE.INI file (meant to be read-only)
	Var /GLOBAL TSTOREVER
	${GetFileVersion} "$INSTDIR\TStore.exe" $TSTOREVER
	WriteINIStr "$INSTDIR\TStore.ini" "General" "Version" "$TSTOREVER"
	WriteINIStr "$INSTDIR\TStore.ini" "General" "Path" "$INSTDIR"
	
	; Create Shortcut
	CreateShortcut "$SMPROGRAMS\$ProductName.lnk" "$INSTDIR\TStore.exe" "" "$INSTDIR\TStore.exe" 0 SW_SHOWNORMAL "" "$ProductName stores the locations of your games and trainers in an easy to use format"
	
	; Create $ProductName Registry entries
	WriteRegStr HKLM "Software\$CompanyName\$ProductName" "Installation Folder" "$INSTDIR"
	WriteRegStr HKLM "Software\$CompanyName\$ProductName" "Product Version" "$TSTOREVER"
	WriteRegStr HKLM "Software\$CompanyName\$ProductName" "SMFolder" "$SMPROGRAMS"
	WriteRegStr HKLM "Software\$CompanyName\$ProductName" "Uninstall Key Location" "Software\Microsoft\Windows\CurrentVersion\Uninstall\$CompanyName $ProductName"
	
SectionEnd
Section "Source Code" Src
	; Install the Source Code
	DetailPrint "Installing Source Code"
	Var /GLOBAL SRCCODE
	StrCpy $SRCCODE "$INSTDIR\Source"
	CreateDirectory "$SRCCODE"
	SetOutPath "$SRCCODE"
	
	; $ProductName Source Code
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
	
	; The Microsoft Redist files are not included in the Patch installer


SectionEnd

; The Patch installer does not contain Microsoft Redist

Section "-Unistall Registry Keys" Unins
	; Create Uninstall Registry Entries
	DetailPrint "Creating Uninstaller"
	Var /GLOBAL UNINSREG
	StrCpy $UNINSREG "Software\Microsoft\Windows\CurrentVersion\Uninstall\$CompanyName $ProductName"
	
	; Get $ProductName file version for the Uninstall Registry key
	${GetFileVersion} "$INSTDIR\TStore.exe" $R0
	
	; Write the keys to the Registry
	WriteRegStr HKLM $UNINSREG "DisplayName" "$CompanyName $ProductName"
	WriteRegStr HKLM $UNINSREG "DisplayVersion" $R0
	WriteRegStr HKLM $UNINSREG "UninstallString" "$INSTDIR\uninstall.exe"
	WriteRegStr HKLM $UNINSREG "Publisher" "$CompanyName"
	
	; Write our Uninstaller
	WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd

; This runs when the program is uninstalling
; Even with the Patch Installer, you MUST have the COMPLETE uninstall script (NSIS does not log actions like other tools such as Inno Setup or Windows Installer)
Section "Uninstall"
	; Switch Start Menu locations
	SetShellVarContext all
	
	; Grab the proper installation folder (in case the uninstaller is in the wrong place)
	ReadRegStr $INSTDIR HKLM "Software\$CompanyName\$ProductName" "Installation Folder"
	
	; Remove our Registry keys
	DetailPrint "Deleting Registry Keys"
	DeleteRegKey HKLM "Software\Microsoft Windows\CurrentVersion\Uninstall\$CompanyName $ProductName"
	DeleteRegKey HKLM "Software\$CompanyName\$ProductName"
	DeleteRegKey /ifempty HKLM "Software\$CompanyName"
	
	; Remove $ProductName's executable
	DetailPrint "Uninstalling $ProductName"
	RMDir /r /REBOOTOK "$INSTDIR\Source"
	Delete "$INSTDIR\TStore.exe"
	Delete "$INSTDIR\TStore.exe.manifest"
	Delete "$INSTDIR\License.rtf"
	Delete "$INSTDIR\README.txt"
	Delete "$INSTDIR\TStore.ini"
	Delete "$INSTDIR\uninstall.exe"
	RMDir "$INSTDIR" ; DO NOT do a /r recursive search (in case, for example, someone installed to C:\Program Files this would really mess up the users computer)
	RMDir $PROGRAMFILES\$CompanyName ;AGAIN, do NOT use /r
	
	DetailPrint "Deleting Shortcuts"
	Delete "$SMPROGRAMS\$ProductName.lnk"
	
	; We are DONE with uninstall
	; As a reminder, DO NOT TOUCH any of the Microsoft DLLs that get installed
	; Also, $ProductName's per-user files do not get uninstalled (it's just not possible to remove files for users other than your own in the main uninstaller, however it is possible to release a utility that does PER-USER cleaning)
	
SectionEnd

; Section Descriptions
LangString DESC_TStoreBase ${LANG_ENGLISH} "$ProductName stores the locations of games and trainers"
LangString DESC_Src ${LANG_ENGLISH} "Source Code for $ProductName (Visual Basic 6)"
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${TStoreBase} $(DESC_TStoreBase)
	!insertmacro MUI_DESCRIPTION_TEXT ${Src} $(DESC_Src)
!insertmacro MUI_FUNCTION_DESCRIPTION_END


; We need to do some checks when the installer and uninstaller are started.

; Tests for Admin rights NT4/2000/XP
; Also works if somehow UAC is disabled or broken on Vista+
; Also, on Windows NT 4.0, the Visual Basic runtime requires Service Pack 3 or later installed
Function .onInit
	; Company and Product
	StrCpy $CompanyName "RaggieSoft"
	StrCpy $ProductName "StoreTrainer"
	
	; Redefine the install folder (Setup will cross-check it later)
	StrCpy $INSTDIR "$PROGRAMFILES\$CompanyName\$ProductName"
	
	; Check for NT Admin Rights
	UserInfo::GetAccountType
	pop $0
	${If} $0 != "admin" ;Require admin rights on NT4+
		MessageBox MB_ICONSTOP|MB_OK "Administrator rights required to install $ProductName!" /SD IDOK
		SetErrorLevel 740 ;ERROR_ELEVATION_REQUIRED
		Quit
	${EndIf}

	; Check for NT 4.0 SP3 or higher
	; Otherwise, $ProductName *SHOULD* be able to run on any other 32bit or 64bit OS
	; That is Windows 95 or later.  Sorry Windows 3.1
	${If} ${IsNT}
		${If} ${IsWinNT4}
			${If} ${AtMostServicePack} 2
				MessageBox MB_ICONSTOP|MB_OK "Service Pack 3 or higher is required for Windows NT 4.0" /SD IDOK
				SetErrorLevel 3 ;3 because Service Pack 3 (pun intended)
				Quit
			${EndIf}
		${EndIf}
	${EndIf}
	
	; Since this is a Patch installer, check for a previous version of $ProductName
	ReadRegStr $PreviousInstallFolder HKLM "Software\$CompanyName\$ProductName" "Installation Folder"
	IfFileExists "$PreviousInstallFolder\TStore.exe" file_found file_not_found
	file_not_found:
		MessageBox mb_iconstop "This patch utility requires that you have $CompanyName $ProductName already installed!" /SD IDOK
		SetErrorLevel 7
		Quit
	file_found:
	${If} $PreviousInstallFolder == ""
		MessageBox mb_iconstop "This installer requires a previous version of $ProductName to be installed!" /SD IDOK
		SetErrorLevel 4
		Quit
	${Else}
		StrCpy $INSTDIR $PreviousInstallFolder
		${GetFileVersion} "$INSTDIR\TStore.exe" $TStoreVersionInstalled
		SetOutPath $TEMP
		File "..\Build\TStore.exe"
		${GetFileVersion} "$TEMP\TStore.exe" $TStoreVersionPatch
		${VersionCompare} "$TStoreVersionInstalled" "$TStoreVersionPatch" $TSVerCompare
		Delete "$TEMP\TStore.exe"
		
		${If} $TSVerCompare == 0
			MessageBox mb_iconstop "This patch is the same version of $ProductName you have!" /SD IDOK
			SetErrorLevel 5
			Quit
		${ElseIf} $TSVerCompare == 1
			MessageBox mb_iconstop "You have a newer version of $ProductName installed!" /SD IDOK
			SetErrorLevel 6
			Quit
		${EndIf}
	${EndIf}
FunctionEnd
Function un.onInit
	; Company and Product
	StrCpy $CompanyName "RaggieSoft"
	StrCpy $ProductName "StoreTrainer"
	UserInfo::GetAccountType
	pop $0
	${If} $0 != "admin" ;Require admin rights on NT4+
		MessageBox mb_iconstop "Administrator rights required to uninstall $ProductName!" /SD IDOK
		SetErrorLevel 740 ;ERROR_ELEVATION_REQUIRED
		Quit
	${EndIf}
FunctionEnd