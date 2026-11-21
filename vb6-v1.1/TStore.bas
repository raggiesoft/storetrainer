Attribute VB_Name = "TStoreMod"
Option Explicit

' ==============================================================================
' FILE: TStore.bas (Module Name: TStoreMod)
' PROJECT: RaggieSoft StoreTrainer v1.1 (Classic Visual Basic 6.0 Edition)
' TARGET OS: Windows 95, 98, ME, NT 4.0 SP3+, 2000 SP4, XP, Vista, 7, 8.1, 10, 11
' LICENSE: MIT License (Code) / CC BY-SA 4.0 (Documentation)
'
' PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
'   What is this file?
'     In Visual Basic 6.0 (VB6), 'TStore.vbp' sets 'Startup="Sub Main"', meaning
'     Windows runs 'Sub Main()' inside this module BEFORE opening any window!
'
'   What did StoreTrainer v1.1 upgrade here compared to v1.0?
'     1. Named Win32 Mutex ('RaggieSoft_StoreTrainer_Mutex'):
'        Calls 'CreateMutexA' in 'kernel32.dll' (with zero backslash prefixes so
'        it works all the way back to Windows 95!). Both our NSIS v1.1 patcher
'        AND StoreTrainer 2.0's Inno Setup installer check this exact Mutex name
'        to detect if 'TStore.exe' is currently running during an upgrade.
'     2. Completed 'CheckCommandLine()' Parser:
'        - '/l "C:\...\Game.stf"': Loads the '.stf' profile, launches the game +
'          trainer immediately, and exits via 'ExitProcess' without flashing the
'          editor window on screen!
'        - '/e "C:\...\Game.stf"': Opens 'frmMain' pre-loaded with that '.stf'.
'        - '"C:\...\Game.stf"' (double-clicked in Windows Explorer): Launches!
' ==============================================================================

' Enables Windows XP+ themed Common Controls when paired with TStore.exe.manifest
Public Declare Function InitCommonControls Lib "comctl32.dll" () As Long

' Cleanly terminates the Win32 process with an integer exit code (preferable to VB6 'End')
Public Declare Sub ExitProcess Lib "kernel32" (ByVal uExitCode As Long)

' Creates a named kernel Mutex object across Windows 95 through Windows 11
Public Declare Function CreateMutexA Lib "kernel32" ( _
    ByVal lpMutexAttributes As Long, _
    ByVal bInitialOwner As Long, _
    ByVal lpName As String) As Long

' Closes a Win32 kernel handle cleanly when shutting down
Public Declare Function CloseHandle Lib "kernel32" (ByVal hObject As Long) As Long

' Module-level handle keeping 'RaggieSoft_StoreTrainer_Mutex' alive while TStore.exe runs
Private g_hAppMutex As Long

' Shared path populated when '/e "file.stf"' is passed on the command line
Public g_strPendingEditFile As String

Sub Main()
    ' Step 1: Register our named Win32 Mutex so NSIS & Inno Setup installers know we are open
    g_hAppMutex = CreateMutexA(0&, 0&, "RaggieSoft_StoreTrainer_Mutex")

    ' Step 2: Turn on Windows XP+ Visual Styles before any form/dialog loads
    InitCommonControls

    ' Step 3: Ensure %APPDATA%\RaggieSoft\StoreTrainer\TStore.ini exists
    If GetStoreTrainerDataFile = "" Then
        CreateStoreTrainerData
    End If

    ' Step 4: Inspect Command$ for '/l', '/e', or a double-clicked '.stf' StoreFile!
    Dim intCmdReturn As Integer
    intCmdReturn = CheckCommandLine(Command$)

    ' Return code 2 means '/l' (or Explorer double-click) already launched the StoreFile:
    ' close our Mutex handle and exit cleanly without opening frmMain!
    If intCmdReturn = 2 Then
        ReleaseAppMutex
        ExitProcess 0
    ElseIf intCmdReturn < 0 Then
        ' Negative return indicates a command-line launch error already reported to the user
        ReleaseAppMutex
        ExitProcess Abs(intCmdReturn)
    End If

    ' Step 5: Refresh stored App.Path inside TStore.ini if TStore.exe moved folders
    Dim strCurPath As String
    strCurPath = IniGetValueString(GetStoreTrainerDataFile, "General", "Path", "PATH_NAME")
    If strCurPath <> App.Path Then
        IniWriteValueString GetStoreTrainerDataFile, "General", "Path", App.Path
    End If
    IniWriteValueString GetStoreTrainerDataFile, "General", "Version", App.Major & "." & App.Minor & "." & App.Revision

    ' Step 6: Ensure 'My Documents\My StoreFiles' exists
    Dim strSTFFolder As String
    strSTFFolder = GetStoreFileFolder(True, True)
    If (strSTFFolder = "NO_FOLDER_CREATED") Then
        MsgBox App.ProductName & " requires that you create a StoreFile folder. Now exiting!", vbCritical, App.ProductName
        ReleaseAppMutex
        ExitProcess 1
    End If

    ' Step 7: Show the main StoreFile Editor window (and load g_strPendingEditFile if '/e' was used)
    frmMain.Show
    If g_strPendingEditFile <> "" Then
        frmMain.LoadStoreFileIntoForm g_strPendingEditFile
    End If
End Sub

' Releases the Win32 Mutex handle before process exit
Public Sub ReleaseAppMutex()
    If g_hAppMutex <> 0& Then
        CloseHandle g_hAppMutex
        g_hAppMutex = 0&
    End If
End Sub

' ==============================================================================
' CheckCommandLine:
'   Parses VB6's raw Command$ string.
'   Returns:
'      0 = No command-line switch; open normal blank editor
'      1 = '/e <file.stf>' -> open editor with <file.stf> loaded
'      2 = '/l <file.stf>' (or bare '<file.stf>') -> launched immediately!
'     <0 = Error code (file missing or launch failed)
' ==============================================================================
Public Function CheckCommandLine(Optional ByVal CommandLine As String = "") As Integer
    Dim strTrimmed As String
    Dim strSwitch As String
    Dim strTargetFile As String
    Dim iOpenRet As Integer
    Dim iLaunchRet As Integer

    g_strPendingEditFile = ""
    strTrimmed = Trim$(CommandLine)
    If strTrimmed = "" Then
        CheckCommandLine = 0
        Exit Function
    End If

    ' Check if the command line starts with '/l', '-l', '/e', or '-e'
    If Len(strTrimmed) >= 3 And (Left$(strTrimmed, 1) = "/" Or Left$(strTrimmed, 1) = "-") Then
        strSwitch = LCase$(Left$(strTrimmed, 2))
        strTargetFile = StripSurroundingQuotes(Trim$(Mid$(strTrimmed, 3)))
    Else
        ' Bare file path (e.g., double-clicking 'Doom.stf' in Windows Explorer) -> treat as '/l'
        strSwitch = "/l"
        strTargetFile = StripSurroundingQuotes(strTrimmed)
    End If

    If strTargetFile = "" Then
        CheckCommandLine = 0
        Exit Function
    End If

    ' Edit Mode ('/e' or '-e')
    If strSwitch = "/e" Or strSwitch = "-e" Then
        g_strPendingEditFile = strTargetFile
        CheckCommandLine = 1
        Exit Function
    End If

    ' Launch Mode ('/l' or '-l')
    If strSwitch = "/l" Or strSwitch = "-l" Then
        SetLaunchingSTF True
        iOpenRet = OpenStoreFile(strTargetFile)
        If iOpenRet <> 0 Then
            MsgBox "Unable to open StoreFile (" & strTargetFile & "). Error code: " & iOpenRet, vbExclamation, App.ProductName
            CheckCommandLine = -2
            Exit Function
        End If

        SetFileName strTargetFile
        isSilentDirtyReset
        AddRecentStoreFile strTargetFile

        iLaunchRet = Launch()
        If iLaunchRet <> 0 Then
            MsgBox "StoreTrainer could not launch the game or trainer (Code " & iLaunchRet & "). Please edit the StoreFile to check its file paths.", vbExclamation, App.ProductName
            CheckCommandLine = -3
            Exit Function
        End If

        CheckCommandLine = 2
        Exit Function
    End If

    CheckCommandLine = 0
End Function

' Helper stripping outer double-quotes from a Windows command-line path argument
Private Function StripSurroundingQuotes(ByVal sValue As String) As String
    Dim sWork As String
    sWork = Trim$(sValue)
    If Len(sWork) >= 2 Then
        If Left$(sWork, 1) = Chr$(34) And Right$(sWork, 1) = Chr$(34) Then
            sWork = Mid$(sWork, 2, Len(sWork) - 2)
        End If
    End If
    StripSurroundingQuotes = sWork
End Function

'Displays About & Legal Notice dialog (MIT Code License + Fictitious Entities Notice)
Public Sub AboutStoreTrainer()
    Dim sMsg As String
    sMsg = App.ProductName & " v" & App.Major & "." & App.Minor & "." & App.Revision & _
           " (Classic Retro Edition — Windows 95 through Windows 11)" & vbCrLf & vbCrLf & _
           "Copyright (C) 2018-2026 Michael P. Ragsdale (RaggieSoft)" & vbCrLf & _
           "Code License: MIT License | Website: https://storetrainer.raggiesoft.com" & vbCrLf & vbCrLf & _
           "Work of Fiction Notice: While RaggieSoft StoreTrainer is a real desktop utility, " & _
           "all narrative characters, bands, universities (CPI, VDU), corporations, transit agencies, " & _
           "and LLC/Trust entities across RaggieSoft creative properties are works of fiction."
    MsgBox sMsg, vbInformation + vbOKOnly, "About " & App.ProductName
End Sub

' Creates %APPDATA%\RaggieSoft\StoreTrainer\TStore.ini on first launch
Public Sub CreateStoreTrainerData()
    Dim FSO As FileSystemObject
    Set FSO = CreateObject("Scripting.FileSystemObject")
    If Not FSO.FolderExists(FGetSpecialFolder(CSIDL_APPDATA) & "\RaggieSoft") Then
        MkDir FGetSpecialFolder(CSIDL_APPDATA) & "\RaggieSoft"
    End If
    If Not FSO.FolderExists(FGetSpecialFolder(CSIDL_APPDATA) & "\RaggieSoft\StoreTrainer") Then
        MkDir FGetSpecialFolder(CSIDL_APPDATA) & "\RaggieSoft\StoreTrainer"
    End If

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
