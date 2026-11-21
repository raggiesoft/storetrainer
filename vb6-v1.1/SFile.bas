Attribute VB_Name = "SFile"
Option Explicit

' ==============================================================================
' FILE: SFile.bas
' PROJECT: RaggieSoft StoreTrainer v1.1 (Classic Visual Basic 6.0 Edition)
' LICENSE: MIT License (Code) / CC BY-SA 4.0 (Documentation)
'
' PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
'   What is this file?
'     'SFile.bas' manages reading, writing, and launching '.stf' StoreFiles!
'     Each '.stf' file is a human-readable Windows INI file storing the path
'     and command-line switches for a PC game and its companion trainer.
'
'   Key Bug Fixes & Features Added in StoreTrainer v1.1:
'     1. Backward-Compatible Version Check ('OpenStoreFile'):
'        v1.0 rejected any '.stf' file whose version was less than the current
'        version ('iSTFCompare = -1'). v1.1 accepts BOTH '1.0' and '1.1' '.stf'
'        files seamlessly (and also interoperates with StoreTrainer 2.0!).
'     2. Working Directory Fix ('ChDir FSO.GetParentFolderName(...)'):
'        In v1.0, 'ChDir GetGamePath()' passed a '.exe' file path instead of its
'        parent folder, raising VB6 Run-time error 76 ('Path not found'). v1.1
'        extracts 'FSO.GetParentFolderName(...)' first so classic games find
'        their data WADs/PK3s/DLLs reliably.
'     3. Multi-Flag Command-Line Switch Fix:
'        In v1.0, switches were wrapped inside extra Chr$(34) quotes, turning
'        '-windowed -novid' into a single literal argument. v1.1 appends switches
'        cleanly without outer quotes.
'     4. Configurable Launch Delay ('LaunchDelaySeconds', 0–30s) & Recent Files MRU:
'        Adds '[General] LaunchDelay' using Win32 'Sleep' in 100ms slices with
'        'DoEvents' so Windows 95/98/2000 stays responsive, plus '[Recent] File1..4'.
' ==============================================================================

' Win32 Sleep API (pauses execution in milliseconds without 100% CPU busy-waiting)
Private Declare Sub Sleep Lib "kernel32" (ByVal dwMilliseconds As Long)

' In-memory state of the currently loaded StoreFile
Private isFileDirty As Boolean
Private strFileName As String
Private TrainerLaunchesGame As Boolean
Private LaunchGameFirst As Boolean
Private LaunchDelaySeconds As Long
Private GamePath As String, GameSwitch As String
Private TrainerPath As String, TrainerSwitch As String
Private doLaunch As Boolean ' Fast-exit flag during '/l' silent StoreFile launches

Public Function GetLaunchingSTF() As Boolean
    GetLaunchingSTF = doLaunch
End Function

Public Sub SetLaunchingSTF(boolDoLaunch As Boolean)
    doLaunch = boolDoLaunch
End Sub

' Clears dirty flag without touching frmMain (used by Sub Main during '/l' silent launch)
Public Sub isSilentDirtyReset()
    isFileDirty = False
End Sub

' Returns the '.stf' schema version string.
' WORD 97 <-> WORD 2000 FORWARD-COMPATIBILITY DESIGN:
'   StoreTrainer v1.0 checks 'CheckVersion(fileVer, "1.0")' and refuses to open
'   any '.stf' file if 'STF.Version' is higher than "1.0"! By keeping 'STF.Version=1.0'
'   in StoreTrainer v1.1 while storing optional v1.1 fields like 'LaunchDelay',
'   any '.stf' saved in v1.1 still opens 100% cleanly inside original StoreTrainer v1.0
'   (just ignoring the extra v1.1 keys, exactly like opening a Word 2000 .doc in Word 97)!
Public Function GetSTFVersionStr() As String
    GetSTFVersionStr = "1.0"
End Function

' Resets memory state for File -> New
Public Sub NewStoreFile()
    SetLaunchGameFirst False
    SetTrainerLaunchesGame False
    SetLaunchDelaySeconds 0
    SetGamePath ""
    SetGameSwitch ""
    SetTrainerPath ""
    SetTrainerSwitch ""
    SetFileName "untitled"
End Sub

Public Function GetLaunchGameFirst() As Boolean
    GetLaunchGameFirst = LaunchGameFirst
End Function

Public Sub SetLaunchGameFirst(boolLaunchGameFirst As Boolean)
    LaunchGameFirst = boolLaunchGameFirst
End Sub

Public Sub SetTrainerLaunchesGame(boolLaunchType As Boolean)
    TrainerLaunchesGame = boolLaunchType
    If boolLaunchType = True Then
        LaunchGameFirst = False
    End If
End Sub

Public Function GetTrainerLaunchesGame() As Boolean
    GetTrainerLaunchesGame = TrainerLaunchesGame
End Function

Public Function GetLaunchDelaySeconds() As Long
    GetLaunchDelaySeconds = LaunchDelaySeconds
End Function

Public Sub SetLaunchDelaySeconds(ByVal lngSeconds As Long)
    If lngSeconds < 0 Then lngSeconds = 0
    If lngSeconds > 30 Then lngSeconds = 30
    LaunchDelaySeconds = lngSeconds
End Sub

Public Function GetGamePath() As String
    GetGamePath = GamePath
End Function

Public Sub SetGamePath(strGamePath As String)
    GamePath = strGamePath
End Sub

Public Function GetTrainerPath() As String
    GetTrainerPath = TrainerPath
End Function

Public Sub SetTrainerPath(strTrainerPath As String)
    TrainerPath = strTrainerPath
End Sub

Public Function GetGameSwitch() As String
    GetGameSwitch = GameSwitch
End Function

Public Sub SetGameSwitch(strGameSwitch As String)
    GameSwitch = strGameSwitch
End Sub

Public Function GetTrainerSwitch() As String
    GetTrainerSwitch = TrainerSwitch
End Function

Public Sub SetTrainerSwitch(strTrainerSwitch As String)
    TrainerSwitch = strTrainerSwitch
End Sub

' Updates window title bar ('*' indicates unsaved edits)
Public Sub SetFileFlag(dirtyFlag As Boolean, frmName As Form)
    On Error GoTo ErrorTrap
    If dirtyFlag = True Then
        isFileDirty = True
        frmName.Caption = GetFileName(True) & " * - " & App.ProductName
    Else
        isFileDirty = False
        frmName.Caption = GetFileName(True) & " - " & App.ProductName
    End If
    Exit Sub
ErrorTrap:
    MsgBox Err.Number & ": " & Err.Description, vbExclamation, App.ProductName
End Sub

Public Function GetFileFlag() As Boolean
    GetFileFlag = isFileDirty
End Function

' Writes the current StoreFile to disk in backward-compatible INI format
Public Function SaveStoreFile(frmName As Form, strTargetFile As String) As Integer
    On Error GoTo ErrorTrapper

    IniWriteValueString strTargetFile, "General", "STF.Version", GetSTFVersionStr
    IniWriteValueString strTargetFile, "General", "LaunchDelay", CStr(GetLaunchDelaySeconds())

    Dim strTrainerLaunchesGame As String
    strTrainerLaunchesGame = IIf(GetTrainerLaunchesGame(), "true", "false")
    IniWriteValueString strTargetFile, "Trainer", "TrainerLaunchesGame", strTrainerLaunchesGame

    Dim strLaunchGameFirst As String
    strLaunchGameFirst = IIf(GetLaunchGameFirst() And Not GetTrainerLaunchesGame(), "true", "false")
    IniWriteValueString strTargetFile, "Game", "LaunchGameFirst", strLaunchGameFirst

    IniWriteValueString strTargetFile, "Game", "Path", GetGamePath
    IniWriteValueString strTargetFile, "Game", "Switch", GetGameSwitch

    IniWriteValueString strTargetFile, "Trainer", "Path", GetTrainerPath
    IniWriteValueString strTargetFile, "Trainer", "Switch", GetTrainerSwitch

    AddRecentStoreFile strTargetFile
    SetFileFlag False, frmName
    SaveStoreFile = 0
    Exit Function

ErrorTrapper:
    SaveStoreFile = Err.Number
End Function

' Opens a '.stf' StoreFile into memory (supports both v1.0 and v1.1 '.stf' files!)
Public Function OpenStoreFile(strFilePath As String) As Integer
    Dim FSO As FileSystemObject
    Set FSO = CreateObject("Scripting.FileSystemObject")

    If Not FSO.FileExists(strFilePath) Then
        OpenStoreFile = 53 ' VB6 File Not Found
        Exit Function
    End If

    Dim strSTFVersion As String
    strSTFVersion = IniGetValueString(strFilePath, "General", "STF.Version", "1.0")

    ' Reject ONLY if the file requires a major version newer than '1.1' (e.g., '3.0').
    ' Older '1.0' files (iSTFCompare = -1) load with 100% backward compatibility!
    Dim iSTFCompare As Integer
    iSTFCompare = CheckVersion(strSTFVersion, GetSTFVersionStr)
    If iSTFCompare = 1 Then
        OpenStoreFile = 1
        Exit Function
    End If

    Dim strTrainerLaunchesGame As String
    strTrainerLaunchesGame = LCase$(IniGetValueString(strFilePath, "Trainer", "TrainerLaunchesGame", "false"))
    SetTrainerLaunchesGame (strTrainerLaunchesGame = "true" Or strTrainerLaunchesGame = "1")

    Dim strLaunchGameFirst As String
    strLaunchGameFirst = LCase$(IniGetValueString(strFilePath, "Game", "LaunchGameFirst", "false"))
    If GetTrainerLaunchesGame() Then
        SetLaunchGameFirst False
    Else
        SetLaunchGameFirst (strLaunchGameFirst = "true" Or strLaunchGameFirst = "1")
    End If

    SetLaunchDelaySeconds CLng(Val(IniGetValueString(strFilePath, "General", "LaunchDelay", "0")))

    SetGamePath IniGetValueString(strFilePath, "Game", "Path", vbNullString)
    SetGameSwitch IniGetValueString(strFilePath, "Game", "Switch", vbNullString)

    SetTrainerPath IniGetValueString(strFilePath, "Trainer", "Path", vbNullString)
    SetTrainerSwitch IniGetValueString(strFilePath, "Trainer", "Switch", vbNullString)

    OpenStoreFile = 0
End Function

Public Sub SetFileName(strLocalFileName As String)
    strFileName = strLocalFileName
End Sub

Public Function GetFileName(Optional showFileNameOnly As Boolean) As String
    On Error GoTo cantReadRegistry
    Dim strLocalFileName As String
    strLocalFileName = strFileName
    Dim boolHideExtensions As Boolean

    If showFileNameOnly = True Then
        Dim FSO As FileSystemObject
        Set FSO = CreateObject("Scripting.FileSystemObject")
        boolHideExtensions = CBool(Val(RegGetValue$(HKEY_CURRENT_USER, "Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt")))
        If boolHideExtensions Then
            strLocalFileName = FSO.GetBaseName(strLocalFileName)
        Else
            strLocalFileName = FSO.GetFileName(strLocalFileName)
        End If
        GetFileName = strLocalFileName
        Exit Function
    End If

    GetFileName = strFileName
    Exit Function
cantReadRegistry:
    boolHideExtensions = False
    Resume Next
End Function

' ==============================================================================
' RECENT FILES (MRU) HELPERS — Stored in %APPDATA%\RaggieSoft\StoreTrainer\TStore.ini
' ==============================================================================
Public Sub AddRecentStoreFile(ByVal sPath As String)
    Dim sIni As String
    Dim arrExisting(1 To 4) As String
    Dim arrUpdated(1 To 4) As String
    Dim i As Integer, outIdx As Integer

    sIni = GetStoreTrainerDataFile()
    If sIni = "" Or Trim$(sPath) = "" Or LCase$(Trim$(sPath)) = "untitled" Then Exit Sub

    For i = 1 To 4
        arrExisting(i) = IniGetValueString(sIni, "Recent", "File" & CStr(i), "")
    Next i

    arrUpdated(1) = sPath
    outIdx = 2
    For i = 1 To 4
        If outIdx <= 4 Then
            If arrExisting(i) <> "" And LCase$(arrExisting(i)) <> LCase$(sPath) Then
                arrUpdated(outIdx) = arrExisting(i)
                outIdx = outIdx + 1
            End If
        End If
    Next i

    For i = 1 To 4
        IniWriteValueString sIni, "Recent", "File" & CStr(i), arrUpdated(i)
    Next i
End Sub

Public Function GetRecentStoreFile(ByVal indexOneToFour As Integer) As String
    Dim sIni As String
    sIni = GetStoreTrainerDataFile()
    If sIni = "" Or indexOneToFour < 1 Or indexOneToFour > 4 Then
        GetRecentStoreFile = ""
        Exit Function
    End If
    GetRecentStoreFile = IniGetValueString(sIni, "Recent", "File" & CStr(indexOneToFour), "")
End Function

' Pauses between launching the first and second program while pumping Win32 events
Private Sub ResponsiveDelaySeconds(ByVal lngSeconds As Long)
    Dim totalLoops As Long, k As Long
    If lngSeconds <= 0 Then Exit Sub
    totalLoops = lngSeconds * 10
    For k = 1 To totalLoops
        Sleep 100
        DoEvents
    Next k
End Sub

' Safely changes drive + working directory to the parent folder of an executable
Private Sub SetWorkingFolderForExe(ByVal sExePath As String, ByRef FSO As FileSystemObject)
    On Error Resume Next
    Dim sParent As String
    sParent = FSO.GetParentFolderName(sExePath)
    If sParent <> "" Then
        ChDrive sParent
        ChDir sParent
    End If
    On Error GoTo 0
End Sub

' Launches the configured Game and Trainer programs
Public Function Launch() As Integer
    On Error GoTo LaunchError
    Dim FSO As FileSystemObject
    Set FSO = CreateObject("Scripting.FileSystemObject")

    ' 1. File must be saved first
    If GetFileFlag = True Or LCase$(GetFileName) = "untitled" Then
        Launch = 1
        Exit Function
    End If

    ' 2. Verify Trainer executable exists
    If DoesFileExist(GetTrainerPath) = False Then
        Launch = 2
        Exit Function
    End If

    ' 3. Verify Game executable exists (unless the Trainer itself launches the Game)
    If GetTrainerLaunchesGame = False Then
        If DoesFileExist(GetGamePath) = False Then
            Launch = 3
            Exit Function
        End If
    End If

    ' 4. Build command lines: quote the executable path ONLY, and append switches unquoted
    '    so multi-flag switches like '-windowed -novid' parse as separate arguments!
    Dim strGameAndSwitch As String, strTrainerAndSwitch As String
    strGameAndSwitch = Chr$(34) & GetGamePath & Chr$(34)
    If Trim$(GetGameSwitch) <> "" Then
        strGameAndSwitch = strGameAndSwitch & " " & Trim$(GetGameSwitch)
    End If

    strTrainerAndSwitch = Chr$(34) & GetTrainerPath & Chr$(34)
    If Trim$(GetTrainerSwitch) <> "" Then
        strTrainerAndSwitch = strTrainerAndSwitch & " " & Trim$(GetTrainerSwitch)
    End If

    ' Mode A: Trainer launches the game automatically -> launch Trainer only
    If GetTrainerLaunchesGame = True Then
        SetWorkingFolderForExe GetTrainerPath(), FSO
        Shell strTrainerAndSwitch, vbNormalFocus
        Launch = 0
        Exit Function
    End If

    ' Mode B: Launch Game First -> optional delay -> launch Trainer
    If GetLaunchGameFirst = True Then
        SetWorkingFolderForExe GetGamePath(), FSO
        Shell strGameAndSwitch, vbNormalFocus
        DoEvents
        ResponsiveDelaySeconds GetLaunchDelaySeconds()
        SetWorkingFolderForExe GetTrainerPath(), FSO
        Shell strTrainerAndSwitch, vbNormalFocus
        DoEvents
        Launch = 0
        Exit Function
    End If

    ' Mode C: Launch Trainer first -> optional delay -> launch Game
    SetWorkingFolderForExe GetTrainerPath(), FSO
    Shell strTrainerAndSwitch, vbNormalFocus
    DoEvents
    ResponsiveDelaySeconds GetLaunchDelaySeconds()
    SetWorkingFolderForExe GetGamePath(), FSO
    Shell strGameAndSwitch, vbNormalFocus
    DoEvents
    Launch = 0
    Exit Function

LaunchError:
    Launch = 4
End Function
