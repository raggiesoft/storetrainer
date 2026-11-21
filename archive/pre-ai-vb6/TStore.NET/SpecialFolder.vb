Option Strict Off
Option Explicit On
Module SpecialFolder
	
	
	' Gives us access to Special Folders
	'UPGRADE_WARNING: Structure ITEMIDLIST may require marshalling attributes to be passed as an argument in this Declare statement. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup1050"'
	Declare Function SHGetSpecialFolderLocation Lib "Shell32.dll" (ByVal hwndOwner As Integer, ByVal nFolder As Integer, ByRef pidl As ITEMIDLIST) As Integer
	Declare Function SHGetPathFromIDList Lib "Shell32.dll"  Alias "SHGetPathFromIDListA"(ByVal pidl As Integer, ByVal pszPath As String) As Integer
	
	Public Structure SHITEMID
		Dim cb As Integer
		Dim abID As Byte
	End Structure
	
	Public Structure ITEMIDLIST
		Dim mkid As SHITEMID
	End Structure
	Public Const MAX_PATH As Short = 260
	
	' List of folders
	Public Enum SpecialFolderID
		CSIDL_DESKTOP = &H0s '// The Desktop - virtual folder
		CSIDL_APPDATA = &H1As '// Roaming App Data (anywhere on network)
		CSIDL_COMMONAPPDATA = &H23s '// Common App Data (probably do NOT have permission)
		CSIDL_LOCALAPPDATA = &H1Cs '// Same as App Data, but stays LOCAL to computer
		CSIDL_PROGRAMS = 2 '// Start Menu\Programs (DO NOT CONFUSE WITH THE PROGRAM FILES FOLDER!!)
		CSIDL_CONTROLS = 3 '// Control Panel - virtual folder
		CSIDL_PRINTERS = 4 '// Printers - virtual folder
		CSIDL_DOCUMENTS = 5 '// My Documents
		CSIDL_FAVORITES = 6 '// Favourites
		CSIDL_STARTUP = 7 '// Startup Folder
		CSIDL_RECENT = 8 '// Recent Documents
		CSIDL_SENDTO = 9 '// Send To Folder
		CSIDL_BITBUCKET = 10 '// Recycle Bin - virtual folder
		CSIDL_STARTMENU = 11 '// Start Menu
		CSIDL_DESKTOPFOLDER = 16 '// Desktop folder
		CSIDL_DRIVES = 17 '// My Computer - virtual folder
		CSIDL_NETWORK = 18 '// Network Neighbourhood - virtual folder
		CSIDL_NETHOOD = 19 '// NetHood Folder
		CSIDL_FONTS = 20 '// Fonts folder
		CSIDL_SHELLNEW = 21 '// ShellNew folder
	End Enum
	
	Public Function FGetSpecialFolder(ByRef CSIDL As SpecialFolderID) As String
		Dim sPath As String
		Dim IDL As ITEMIDLIST
		'
		' Retrieve info about system folders such as the "Recent Documents" folder.
		' Info is stored in the IDL structure.
		'
		FGetSpecialFolder = ""
		If SHGetSpecialFolderLocation(GetForegroundWindow(), CSIDL, IDL) = 0 Then
			'
			' Get the path from the ID list, and return the folder.
			'
			sPath = Space(MAX_PATH)
			If SHGetPathFromIDList(IDL.mkid.cb, sPath) Then
				FGetSpecialFolder = Left(sPath, InStr(sPath, vbNullChar) - 1) & ""
			End If
		End If
	End Function
	
	
	Public Function GetStoreFileFolder(Optional ByRef doCreateFolder As Boolean = False, Optional ByRef doPromptCreateFolder As Boolean = False) As String
		Dim FSO As IWshRuntimeLibrary.FileSystemObject
		FSO = CreateObject("Scripting.FileSystemObject")
		Dim msgCreateFolder As MsgBoxResult
		If DoesFolderExist(iniGetValueString(getStoreTrainerDataFile, "Library", "Folder", vbNullString)) Then
			' My Documents\StoreFiles exists.  Give it to the user
			GoTo ReturnFolder
		Else
			' The folder does not exist
			If doCreateFolder Then
				' The user wants to create the folder
				If doPromptCreateFolder Then
					' The user needs a Yes/No
					msgCreateFolder = MsgBox("The StoreFile folder does not exist.  Create it?", MsgBoxStyle.YesNo + MsgBoxStyle.Question, System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName)
					If (msgCreateFolder = MsgBoxResult.Yes) Then
						GoTo CreateFolder
					Else
						GoTo NoFolderCreated
					End If
				End If
			Else
				' Folder wasn't created
				GoTo NoFolderCreated
			End If
		End If
		
		' Creates the folder
CreateFolder: 
		
		' Create the StoreFiles folder
		On Error GoTo NoFolderCreatedShowError
		Dim strTStoreLibrary As String
		Dim winInfo As osInfo
		Dim strMyDocuments As String
		Dim errorLocation As Short
		strMyDocuments = FGetSpecialFolder(SpecialFolderID.CSIDL_DOCUMENTS)
		If DoesFolderExist(strMyDocuments & "\My StoreFiles") = False Then
			FSO.CreateFolder(strMyDocuments & "\My StoreFiles")
		End If
		strTStoreLibrary = strMyDocuments & "\My StoreFiles"
		
		iniWriteValueString(getStoreTrainerDataFile, "Library", "Folder", strTStoreLibrary)
		MsgBox("StoreFile Library successfully created at: " & strTStoreLibrary, MsgBoxStyle.Information, System.Reflection.Assembly.GetExecutingAssembly.GetName.Name)
		
		GoTo ReturnFolder
		
		' Returns the folder path
ReturnFolder: 
		GetStoreFileFolder = iniGetValueString(getStoreTrainerDataFile, "Library", "Folder", "NO_FOLDER_CREATED")
		Exit Function
		
NoFolderCreatedShowError: 
		MsgBox(Err.Number & " - " & Err.Description & vbCrLf & strMyDocuments)
NoFolderCreated: 
		
		' Folder doesn't exist, user didn't create
		GetStoreFileFolder = "NO_FOLDER_CREATED"
		
	End Function
	
	' Does a folder exist?
	Public Function DoesFolderExist(ByRef strFullPath As String) As Boolean
		Dim FSO As IWshRuntimeLibrary.FileSystemObject
		FSO = CreateObject("Scripting.FileSystemObject")
		
		DoesFolderExist = FSO.FolderExists(strFullPath)
	End Function
	
	' Does a file exist?
	Public Function DoesFileExist(ByRef strFullPath As String) As Boolean
		Dim FSO As IWshRuntimeLibrary.FileSystemObject
		FSO = CreateObject("Scripting.FileSystemObject")
		
		DoesFileExist = FSO.FileExists(strFullPath)
	End Function
End Module