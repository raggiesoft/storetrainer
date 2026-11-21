Option Strict Off
Option Explicit On
Imports VB = Microsoft.VisualBasic
Module TStoreMod
	
	
	
	' On Windows XP or later, turns on Visual Styles
	Public Declare Function InitCommonControls Lib "comctl32.dll" () As Integer
	
	'Allows us to have a return code (use in place of an End statement)
	Public Declare Sub ExitProcess Lib "kernel32" (ByVal uExitCode As Integer)
	
	'UPGRADE_WARNING: Application will terminate when Sub Main() finishes. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup1047"'
	Public Sub Main()
		' Checks the command line to see if we have a file to operate on
		' /e filename.stf to Edit a file
		' /l filename.stf to Launch a file
		Dim intCmdReturn As Short
		intCmdReturn = CheckCommandLine(VB.Command())
		
		' Tells StoreTrainer that we want to turn on Visual Styles in Windows XP or later
		InitCommonControls()
		
		' Let's create TStore.INI if necessary
		If GetStoreTrainerDataFile = "" Then
			CreateStoreTrainerData()
		End If
		
		' Let's refresh the StoreTrainer Path if necessary
		' This is primarily used for refreshing when switching from DEBUG to PRODUCTION
		Dim strCurPath As String
		strCurPath = IniGetValueString(GetStoreTrainerDataFile, "General", "Path", "PATH_NAME")
		Dim iMSG As MsgBoxResult
		If strCurPath <> VB6.GetPath Then
			iMSG = MsgBox("The " & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName & " configuration settings need to be updated.  Do that now?", MsgBoxStyle.YesNoCancel + MsgBoxStyle.Question, System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName)
			If iMSG = MsgBoxResult.Cancel Then ExitProcess(4) 'User Canceled updating TSTORE.INI
			If iMSG = MsgBoxResult.Yes Then
				IniWriteValueString(GetStoreTrainerDataFile, "General", "Path", VB6.GetPath)
			End If
		End If
		
		
		' Let's make sure the StoreFile folder exists
		' By default, it should be <MY_DOCUMENTS>\StoreFiles
		Dim strSTFFolder As String
		strSTFFolder = GetStoreFileFolder(True, True)
		If (strSTFFolder = "NO_FOLDER_CREATED") Then
			' Error: folder does not exist, or wasn't created
			MsgBox(System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName & " requires that you create a StoreFile folder.  Now exiting!", MsgBoxStyle.Critical, System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName)
			
			' Exit with a Code 1 ERROR_NO_STF_FOLDER
			ExitProcess(1)
		End If
		' Brings up the main screen
		frmMain.DefInstance.Show()
	End Sub
	
	' This checks for a command line switch being passed in
	' Returns an integer based on results:
	' 0 - no command line switch found, load as normal
	' 1 - File loaded for editing /e filename.stf
	' 2 - File loaded for launching /l filename.stf
	Public Function CheckCommandLine(Optional ByRef CommandLine As String = "") As Short
		
	End Function
	
	Public Sub AboutStoreTrainer()
		'UPGRADE_ISSUE: App property App.Revision was not upgraded. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup2069"'
		MsgBox(System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName & " v" & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMajorPart & "." & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMinorPart & "." & App.Revision & vbCrLf & "" & vbCrLf & "Copyright (C) 2018 - 2021 " & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).CompanyName, MsgBoxStyle.Information + MsgBoxStyle.OKOnly, System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName)
	End Sub
	
	Public Sub CreateStoreTrainerData()
		' First, let's create folders as necessary
		Dim FSO As IWshRuntimeLibrary.FileSystemObject
		FSO = CreateObject("Scripting.FileSystemObject")
		If Not FSO.FolderExists(FGetSpecialFolder(SpecialFolder.SpecialFolderID.CSIDL_APPDATA) & "\RaggieSoft") Then
			MkDir(FGetSpecialFolder(SpecialFolder.SpecialFolderID.CSIDL_APPDATA) & "\RaggieSoft")
		End If
		If Not FSO.FolderExists(FGetSpecialFolder(SpecialFolder.SpecialFolderID.CSIDL_APPDATA) & "\RaggieSoft\StoreTrainer") Then
			MkDir(FGetSpecialFolder(SpecialFolder.SpecialFolderID.CSIDL_APPDATA) & "\RaggieSoft\StoreTrainer")
		End If
		
		' Now, let's work on StoreTrainer data
		'UPGRADE_ISSUE: App property App.Revision was not upgraded. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup2069"'
		IniWriteValueString(GetStoreTrainerDataDir & "\TStore.ini", "General", "Version", System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMajorPart & "." & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMinorPart & "." & App.Revision)
		IniWriteValueString(GetStoreTrainerDataDir & "\TStore.ini", "General", "Path", VB6.GetPath)
	End Sub
	
	Public Function GetStoreTrainerDataDir() As String
		GetStoreTrainerDataDir = FGetSpecialFolder(SpecialFolder.SpecialFolderID.CSIDL_APPDATA) & "\RaggieSoft\StoreTrainer"
	End Function
	Public Function GetStoreTrainerDataFile() As String
		If DoesFileExist(FGetSpecialFolder(SpecialFolder.SpecialFolderID.CSIDL_APPDATA) & "\RaggieSoft\StoreTrainer\TStore.ini") Then
			GetStoreTrainerDataFile = FGetSpecialFolder(SpecialFolder.SpecialFolderID.CSIDL_APPDATA) & "\RaggieSoft\StoreTrainer\TStore.ini"
		Else
			GetStoreTrainerDataFile = ""
		End If
	End Function
End Module