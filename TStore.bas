Attribute VB_Name = "TStoreMod"
Option Explicit



' On Windows XP or later, turns on Visual Styles
Public Declare Function InitCommonControls Lib "comctl32.dll" () As Long

'Allows us to have a return code (use in place of an End statement)
Public Declare Sub ExitProcess Lib "kernel32" (ByVal uExitCode As Long)

Sub Main()
    ' Checks the command line to see if we have a file to operate on
    ' /e filename.stf to Edit a file
    ' /l filename.stf to Launch a file
    Dim intCmdReturn As Integer
    intCmdReturn = CheckCommandLine(Command$)
    
    ' Tells StoreTrainer that we want to turn on Visual Styles in Windows XP or later
    InitCommonControls
    
    ' Let's create TStore.INI if necessary
    If GetStoreTrainerDataFile = "" Then
        CreateStoreTrainerData
    End If
    
    ' Let's refresh the StoreTrainer Path if necessary
    ' This is primarily used for refreshing when switching from DEBUG to PRODUCTION
    Dim strCurPath As String
    strCurPath = IniGetValueString(GetStoreTrainerDataFile, "General", "Path", "PATH_NAME")
    If strCurPath <> App.Path Then
        Dim iMSG As VbMsgBoxResult
        iMSG = MsgBox("The " & App.ProductName & " configuration settings need to be updated.  Do that now?", vbYesNoCancel + vbQuestion, App.ProductName)
        If iMSG = vbCancel Then ExitProcess 4 'User Canceled updating TSTORE.INI
        If iMSG = vbYes Then
            IniWriteValueString GetStoreTrainerDataFile, "General", "Path", App.Path
        End If
    End If
        

    ' Let's make sure the StoreFile folder exists
    ' By default, it should be <MY_DOCUMENTS>\StoreFiles
    Dim strSTFFolder As String
    strSTFFolder = GetStoreFileFolder(True, True)
    If (strSTFFolder = "NO_FOLDER_CREATED") Then
        ' Error: folder does not exist, or wasn't created
        MsgBox App.ProductName & " requires that you create a StoreFile folder.  Now exiting!", vbCritical, App.ProductName
        
        ' Exit with a Code 1 ERROR_NO_STF_FOLDER
        ExitProcess 1
    End If
    ' Brings up the main screen
    frmMain.Show
End Sub

' This checks for a command line switch being passed in
' Returns an integer based on results:
' 0 - no command line switch found, load as normal
' 1 - File loaded for editing /e filename.stf
' 2 - File loaded for launching /l filename.stf
Public Function CheckCommandLine(Optional CommandLine As String = "") As Integer
    
End Function

Public Sub AboutStoreTrainer()
    MsgBox App.ProductName & " v" & App.Major & "." & App.Minor & "." & App.Revision & vbCrLf & "" & vbCrLf & "Copyright (C) 2024-2025 " & App.CompanyName, vbInformation + vbOKOnly, App.ProductName
End Sub

Public Sub CreateStoreTrainerData()
    ' First, let's create folders as necessary
    Dim FSO As FileSystemObject
    Set FSO = CreateObject("Scripting.FileSystemObject")
    If Not FSO.FolderExists(FGetSpecialFolder(CSIDL_APPDATA) & "\RaggieSoft") Then
        MkDir FGetSpecialFolder(CSIDL_APPDATA) & "\RaggieSoft"
    End If
    If Not FSO.FolderExists(FGetSpecialFolder(CSIDL_APPDATA) & "\RaggieSoft\StoreTrainer") Then
        MkDir FGetSpecialFolder(CSIDL_APPDATA) & "\RaggieSoft\StoreTrainer"
    End If
    
    ' Now, let's work on StoreTrainer data
    IniWriteValueString GetStoreTrainerDataDir & "\TStore.ini", "General", "Version", App.Major & "." & App.Minor & "." & App.Revision
    IniWriteValueString GetStoreTrainerDataDir & "\TStore.ini", "General", "Path", App.Path
End Sub

Public Function GetStoreTrainerDataDir() As String
    GetStoreTrainerDataDir = FGetSpecialFolder(CSIDL_APPDATA) & "\RaggieSoft\StoreTrainer"
End Function
Public Function GetStoreTrainerDataFile() As String
    If DoesFileExist(FGetSpecialFolder(CSIDL_APPDATA) & "\RaggieSoft\StoreTrainer\TStore.ini") Then
        GetStoreTrainerDataFile = FGetSpecialFolder(CSIDL_APPDATA) & "\RaggieSoft\StoreTrainer\TStore.ini"
    Else
        GetStoreTrainerDataFile = ""
    End If
End Function
