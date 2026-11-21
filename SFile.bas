Attribute VB_Name = "SFile"
Option Explicit
                 
' Holds our file data temporarily in memory
Private isFileDirty As Boolean
Private strFileName As String

Private StoreFileVersion As String 'YES, As String (file format limitation)

Private TrainerLaunchesGame As Boolean
Private LaunchGameFirst As Boolean

Private GamePath As String, GameSwitch As String
Private TrainerPath As String, TrainerSwitch As String

Private doLaunch As Boolean 'Fast Exit when launching a StoreFile

Public Function GetLaunchingSTF() As Boolean
    GetLaunchingSTF = doLaunch
End Function
Public Sub SetLaunchingSTF(boolDoLaunch As Boolean)
    doLaunch = boolDoLaunch
End Sub
' Returns the Release ID of the STF File Version.  Increment every time we need to update the file format
Public Function GetSTFVersionStr() As String
    GetSTFVersionStr = "1.0"
End Function

' Returns the same as above, but converted to a Long to make math comparison easy
Public Function GetSTFVersionLng() As Long
    GetSTFVersionLng = CLng(GetSTFVersionStr)
End Function

' New StoreFile (remember to work the form)
Public Sub NewStoreFile()
    SetLaunchGameFirst False
    SetTrainerLaunchesGame False
    SetGamePath ""
    SetGameSwitch ""
    SetTrainerPath ""
    SetTrainerSwitch ""
    SetFileName "untitled"
End Sub

' Some trainers expect the game to be launched first
Public Function GetLaunchGameFirst() As Boolean
    GetLaunchGameFirst = LaunchGameFirst
End Function
Public Sub SetLaunchGameFirst(boolLaunchGameFirst As Boolean)
    LaunchGameFirst = boolLaunchGameFirst
End Sub

Public Sub SetTrainerLaunchesGame(boolLaunchType As Boolean)
    TrainerLaunchesGame = boolLaunchType
End Sub
Public Function GetTrainerLaunchesGame() As Boolean
    GetTrainerLaunchesGame = TrainerLaunchesGame
End Function

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
' File Access Functions

' Tells StoreTrainer if the file is saved or not
Public Sub SetFileFlag(dirtyFlag As Boolean, frmName As Form)
    On Error GoTo ErrorTrap
    Dim errorType As String
    errorType = "Not Yet Set"
    If dirtyFlag = True Then
        errorType = "Dirty = true"
        isFileDirty = True
        frmName.Caption = GetFileName(True) & " * - " & App.ProductName
    ElseIf dirtyFlag = False Then
        errorType = "Dirty = false"
        isFileDirty = False
        errorType = errorType & " - Did Set Flag"
        frmName.Caption = GetFileName(True) & " - " & App.ProductName
        errorType = errorType & " - Did (not) Set Form Name"
    Else
        MsgBox "Improper Usage: setFileFlag() requires true or false!", vbExclamation + vbOKOnly, App.ProductName
        errorType = "Improper Usage"
    End If
    Exit Sub
ErrorTrap:
    MsgBox errorType & vbCrLf & Err.Number & ": " & Err.Description
End Sub

' Queries the Dirty state
Public Function GetFileFlag() As Boolean
    GetFileFlag = isFileDirty
End Function


' This writes StoreFile data to disk
Public Function SaveStoreFile(frmName As Form, strFileName As String) As Integer
    On Error GoTo ErrorTrapper
    ' Write StoreFile data to the disk
    
    ' General Info
    IniWriteValueString strFileName, "General", "STF.Version", GetSTFVersionStr
    
    
    ' Trainer Launches Game
    Dim strTrainerLaunchesGame As String
    If GetTrainerLaunchesGame() = True Then
        strTrainerLaunchesGame = "true"
    End If
    If GetTrainerLaunchesGame() = False Then
        strTrainerLaunchesGame = "false"
    End If
    IniWriteValueString strFileName, "Trainer", "TrainerLaunchesGame", strTrainerLaunchesGame
    
    ' Launch Game First
    Dim strLaunchGameFirst As String
    If GetLaunchGameFirst() = True Then
        strLaunchGameFirst = "true"
    End If
    If GetLaunchGameFirst() = False Then
        strLaunchGameFirst = "false"
    End If
    IniWriteValueString strFileName, "Game", "LaunchGameFirst", strLaunchGameFirst
    
    ' Write Game and Trainer info
    IniWriteValueString strFileName, "Game", "Path", GetGamePath
    IniWriteValueString strFileName, "Game", "Switch", GetGameSwitch
    
    IniWriteValueString strFileName, "Trainer", "Path", GetTrainerPath
    IniWriteValueString strFileName, "Trainer", "Switch", GetTrainerSwitch
    
    ' Unset the Dirty flag
    SetFileFlag False, frmName
    
    ' Reports no error on saving
    SaveStoreFile = 0
    Exit Function
    
    ' Error Trapping
ErrorTrapper: '
    
    SaveStoreFile = Err.Number
End Function

' Opens a StoreFile.  NOTE: this does NOT fill in the textboxes on a form
' Any return non-zero is an error
Public Function OpenStoreFile(strFilePath As String) As Integer
    Dim FSO As FileSystemObject
    Set FSO = CreateObject("Scripting.FileSystemObject")
    
    'Let's see if the file exists or not (return 53 (the same as VB6's File Not Found error) if it doesn't)
    If Not FSO.FileExists(strFilePath) Then
        OpenStoreFile = 53
        Exit Function
    End If
    
    ' File does exist.  Let's make sure it's a StoreFile this version of StoreTrainer understands
    Dim strSTFVersion As String
    strSTFVersion = IniGetValueString(strFilePath, "General", "STF.Version", "0.0")
    Dim iSTFCompare As Integer
    iSTFCompare = CheckVersion(strSTFVersion, GetSTFVersionStr)
    
    ' The file being opened expects a newer version of StoreTrainer
    If iSTFCompare = 1 Then
        OpenStoreFile = 1
        Exit Function
    End If
    
    ' The file being opened expects an older version of StoreTrainer
    If iSTFCompare = -1 Then
        OpenStoreFile = 2
        Exit Function
    End If
    
    ' Now, let's load data into memory
    ' THIS WILL NOT LOAD DATA INTO A FORM
    
    ' Load Launch Data
    Dim strTrainerLaunchesGame As String
    strTrainerLaunchesGame = IniGetValueString(strFilePath, "Trainer", "TrainerLaunchesGame", "false")
    
    If strTrainerLaunchesGame = "true" Then
        SetTrainerLaunchesGame True
    Else
        SetTrainerLaunchesGame False
    End If
    
    Dim strLaunchGameFirst As String
    strLaunchGameFirst = IniGetValueString(strFilePath, "Game", "LaunchGameFirst", "false")
    If strLaunchGameFirst = "true" Then
        SetLaunchGameFirst True
    Else
        SetLaunchGameFirst False
    End If
    
    ' Get the Game and Trainer path and switches
    SetGamePath IniGetValueString(strFilePath, "Game", "Path", vbNullString)
    SetGameSwitch IniGetValueString(strFilePath, "Game", "Switch", vbNullString)
    
    SetTrainerPath IniGetValueString(strFilePath, "Trainer", "Path", vbNullString)
    SetTrainerSwitch IniGetValueString(strFilePath, "Trainer", "Switch", vbNullString)
    
    ' We're done. It is up to you to populate the form
    OpenStoreFile = 0
End Function

Public Sub SetFileName(strLocalFileName As String)
    strFileName = strLocalFileName
End Sub

Public Function GetFileName(Optional showFileNameOnly As Boolean) As String
    On Error GoTo cantReadRegistry
    ' Pull the filename from memory
    Dim strLocalFileName As String
    strLocalFileName = strFileName
    Dim boolHideExtensions As Boolean
    If showFileNameOnly = True Then
        ' Needed if the user wants to only show the filename without path
        Dim FSO As FileSystemObject
        Set FSO = CreateObject("Scripting.FileSystemObject")
        ' Return the filename without a path
        ' First, we see if the user wants file extensions hidden or not (it's a user setting in Windows Explorer)
        ' Apparently this is hardened in Vista+ (see cantReadRegistry below)
        boolHideExtensions = CBool(RegGetValue$(HKEY_CURRENT_USER, "Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt"))
        If boolHideExtensions Then
            strLocalFileName = FSO.GetBaseName(strLocalFileName)
        Else
            strLocalFileName = FSO.GetFileName(strLocalFileName)
        End If
        
        ' Return that to the user
        GetFileName = strLocalFileName
        Exit Function
    End If
    ' Return the full filename
    GetFileName = strFileName
    Exit Function
cantReadRegistry:
    ' assume we don't have access to the Registry, we will NOT hide the extensions
    ' Seems to be an issue with newer versions of Windows (because VB6 doesn't understand Registry Security)
    boolHideExtensions = False
    Resume Next
End Function

Public Function Launch() As Integer
    ' Let's launch our StoreFile.  RULES:
    ' * File must NOT be dirty
    ' * Game and Trainer path MUST exist (unless of course, the trainer launches the game)
    ' * Returns an integer based on error type.  Anything non-zero means error
    
    ' Set up the FileSystem object
    Dim FSO As FileSystemObject
    Set FSO = CreateObject("Scripting.FileSystemObject")
    
    ' Is the file dirty?
    If GetFileFlag = True Or GetFileName = "untitled" Then
        Launch = 1 ' File isn't saved.  Return code 1
        Exit Function
    End If
    
    ' Look for the trainer
    If DoesFileExist(GetTrainerPath) = False Then
        Launch = 2 ' Trainer doesn't exist.  Return code 2
        Exit Function
    End If
    
    ' Look for the game (if it's NOT launched by the trainer)
    If GetTrainerLaunchesGame = False Then
        If DoesFileExist(GetGamePath) = False Then
            Launch = 3 ' Game doesn't exist.  Return code 3
            Exit Function
        End If
    End If
    
    ' Checked all our errors.  Let's launch files
    ' Let's build our file paths
    Dim strGameAndSwitch As String, strTrainerAndSwitch As String
    strGameAndSwitch = Chr$(34) & GetGamePath & Chr$(34)
    If GetGameSwitch <> "" Then
        strGameAndSwitch = strGameAndSwitch & " " & Chr$(34) & GetGameSwitch & Chr$(34)
    End If
    strTrainerAndSwitch = Chr$(34) & GetTrainerPath & Chr$(34)
    If GetTrainerSwitch <> "" Then
        strTrainerAndSwitch = strTrainerAndSwitch & " " & Chr$(34) & GetTrainerSwitch & Chr$(34)
    End If
    
    ' We are ready to launch.  Don't forget to get the ParentFolder property because some trainers (and games?) expect the working folder to be set
    
    ' Now to see what to launch
    If GetLaunchGameFirst = True Then
        
        ' Fall back, just in case
        If GetTrainerLaunchesGame = False Then
            ChDrive GetGamePath()
            ChDir GetGamePath()
            Shell strGameAndSwitch, vbNormalFocus
            DoEvents
        End If
        ChDrive GetTrainerPath()
        ChDir GetTrainerPath()
        Shell strTrainerAndSwitch, vbNormalFocus
        DoEvents
        Launch = 0
        Exit Function
    End If
    
    ' We just need to launch the trainer
    If GetTrainerLaunchesGame = True Then
        ChDrive GetTrainerPath()
        ChDir GetTrainerPath()
        Shell strTrainerAndSwitch, vbNormalFocus
        Launch = 0
        Exit Function
    End If
    
    ' None of the options are selected.  Launch both
    ChDrive GetGamePath()
    ChDir GetGamePath()
    Shell strGameAndSwitch, vbNormalFocus
    DoEvents
    ChDrive GetTrainerPath()
    ChDir GetTrainerPath()
    Shell strTrainerAndSwitch, vbNormalFocus
    DoEvents
    Launch = 0
    Exit Function
    
End Function

                 
                 

