VERSION 5.00
Object = "{F9043C88-F6F2-101A-A3C9-08002B2F49FB}#1.2#0"; "comdlg32.ocx"
Begin VB.Form frmMain 
   BorderStyle     =   1  'Fixed Single
   Caption         =   "[UNTITLED] - RaggieSoft StoreTrainer"
   ClientHeight    =   4905
   ClientLeft      =   45
   ClientTop       =   615
   ClientWidth     =   6615
   Icon            =   "frmMain.frx":0000
   LinkTopic       =   "Form1"
   MaxButton       =   0   'False
   ScaleHeight     =   4905
   ScaleWidth      =   6615
   StartUpPosition =   2  'CenterScreen
   Begin VB.CommandButton cmdLaunch 
      Caption         =   "&Launch"
      Height          =   495
      Left            =   5400
      TabIndex        =   16
      Top             =   4320
      Width           =   1095
   End
   Begin VB.Frame fraTrainerData 
      Caption         =   "Trainer Data"
      Height          =   1335
      Left            =   120
      TabIndex        =   10
      Top             =   2880
      Width           =   6375
      Begin VB.TextBox txtTrainerPath 
         Height          =   285
         Left            =   120
         TabIndex        =   12
         Top             =   240
         Width           =   6135
      End
      Begin VB.PictureBox picXPShim3 
         BorderStyle     =   0  'None
         Height          =   975
         Left            =   120
         ScaleHeight     =   975
         ScaleWidth      =   6135
         TabIndex        =   11
         Top             =   240
         Width           =   6135
         Begin VB.CommandButton cmdBrowseTrainer 
            Caption         =   "Browse..."
            Height          =   375
            Left            =   4680
            TabIndex        =   15
            Top             =   360
            Width           =   1335
         End
         Begin VB.TextBox txtTrainerSwitch 
            Height          =   285
            Left            =   720
            TabIndex        =   14
            Top             =   360
            Width           =   3855
         End
         Begin VB.Label lblTrainerSwitch 
            AutoSize        =   -1  'True
            Caption         =   "Switch:"
            Height          =   195
            Left            =   0
            TabIndex        =   13
            Top             =   360
            Width           =   525
         End
      End
   End
   Begin VB.Frame fraGameData 
      Caption         =   "Game Data"
      Height          =   1215
      Left            =   120
      TabIndex        =   4
      Top             =   1560
      Width           =   6375
      Begin VB.PictureBox picXPShim2 
         BorderStyle     =   0  'None
         Height          =   855
         Left            =   120
         ScaleHeight     =   855
         ScaleWidth      =   6135
         TabIndex        =   5
         Top             =   240
         Width           =   6135
         Begin VB.CommandButton cmdBrowseGame 
            Caption         =   "Browse..."
            Height          =   375
            Left            =   4680
            TabIndex        =   9
            Top             =   360
            Width           =   1335
         End
         Begin VB.TextBox txtGameSwitch 
            Height          =   285
            Left            =   720
            TabIndex        =   8
            Top             =   360
            Width           =   3855
         End
         Begin VB.TextBox txtGamePath 
            Height          =   285
            Left            =   0
            TabIndex        =   6
            Top             =   0
            Width           =   6135
         End
         Begin VB.Label lblGameSwitch 
            AutoSize        =   -1  'True
            Caption         =   "Switch:"
            Height          =   195
            Left            =   0
            TabIndex        =   7
            Top             =   360
            Width           =   525
         End
      End
   End
   Begin VB.Frame fraStoreFileDetails 
      Caption         =   "General Data"
      Height          =   1335
      Left            =   120
      TabIndex        =   0
      Top             =   120
      Width           =   6375
      Begin VB.PictureBox picXPShim1 
         BorderStyle     =   0  'None
         Height          =   975
         Left            =   120
         ScaleHeight     =   975
         ScaleWidth      =   6135
         TabIndex        =   1
         Top             =   240
         Width           =   6135
         Begin VB.CheckBox chkLaunchGameFirst 
            Caption         =   "&Launch Game First"
            Height          =   255
            Left            =   120
            TabIndex        =   3
            Top             =   360
            Width           =   2415
         End
         Begin VB.CheckBox chkTrainerLaunchesGame 
            Caption         =   "&Trainer Launches Game"
            Height          =   255
            Left            =   120
            TabIndex        =   2
            Top             =   0
            Width           =   2655
         End
      End
   End
   Begin MSComDlg.CommonDialog cdgStoreFile 
      Left            =   120
      Top             =   4320
      _ExtentX        =   847
      _ExtentY        =   847
      _Version        =   393216
   End
   Begin VB.Menu mnuFile 
      Caption         =   "&File"
      Begin VB.Menu mnuFileNew 
         Caption         =   "&New"
         Shortcut        =   ^N
      End
      Begin VB.Menu mnuFileOpen 
         Caption         =   "&Open..."
         Shortcut        =   ^O
      End
      Begin VB.Menu mnuFileDash1 
         Caption         =   "-"
      End
      Begin VB.Menu mnuFileSave 
         Caption         =   "&Save"
         Shortcut        =   ^S
      End
      Begin VB.Menu mnuFileSaveAs 
         Caption         =   "Save &As..."
      End
      Begin VB.Menu mnuFileDash2 
         Caption         =   "-"
      End
      Begin VB.Menu mnuFileExit 
         Caption         =   "E&xit"
      End
   End
   Begin VB.Menu mnuProject 
      Caption         =   "&Project"
      Begin VB.Menu mnuProjectBrowse 
         Caption         =   "&Browse"
         Begin VB.Menu mnuProjectBrowseGame 
            Caption         =   "&Game..."
         End
         Begin VB.Menu mnuProjectBrowseTrainer 
            Caption         =   "&Trainer..."
         End
      End
      Begin VB.Menu mnuProjectDash1 
         Caption         =   "-"
      End
      Begin VB.Menu mnuProjectSwitch 
         Caption         =   "&Switch"
         Begin VB.Menu mnuProjrectSwitchGame 
            Caption         =   "&Game..."
         End
         Begin VB.Menu mnuProjectSwitchTrainer 
            Caption         =   "&Trainer..."
         End
      End
      Begin VB.Menu mnuProjectDash2 
         Caption         =   "-"
      End
      Begin VB.Menu mnuProjectLaunch 
         Caption         =   "&Launch"
         Shortcut        =   ^L
      End
   End
   Begin VB.Menu mnuHelp 
      Caption         =   "&Help"
      Begin VB.Menu mnuHelpAbout 
         Caption         =   "&About..."
      End
   End
End
Attribute VB_Name = "frmMain"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit


Private Sub Form_Load()
    SetFileName "untitled"
    SetFileFlag False, Me
End Sub



Private Sub Form_QueryUnload(Cancel As Integer, UnloadMode As Integer)
    Dim iMSG As VbMsgBoxResult
    
    ' When a StoreFile is launched, we must quickly close up
    If GetLaunchingSTF = True Then
        Cancel = False
        Exit Sub
    End If
    
    ' A files is not being launched
    If GetFileFlag() = False Then
        iMSG = MsgBox("Are you sure you want to exit " & App.Title & "?", vbQuestion + vbYesNo)
        If iMSG = vbNo Then
            Cancel = True
        End If
        If iMSG = vbYes Then
            Cancel = False
        End If
    Else
        
        iMSG = MsgBox("Would you like to save your work before exiting?", vbQuestion + vbYesNoCancel, App.ProductName)
        If iMSG = vbYes Then
            ' File isn't saved yet.  Show a Save As dialog
            If LCase$(GetFileName()) = "untitled" Then
                Dim strSaveFile As String
                strSaveFile = DisplaySaveAs()
                If strSaveFile = "32755" Then
                    ' User clicked Cancel in the Save As dialog
                    Cancel = True
                    Exit Sub
                End If
                
                ' Write that filename to memory
                SetFileName (strSaveFile)
            End If
            
            ' Write data to disk
            SaveStoreFile Me, GetFileName()
            Cancel = False
        End If
        If iMSG = vbNo Then
            Cancel = False
        End If
        If iMSG = vbCancel Then
            ' Cancel.  No exit, No save.
            Cancel = True
        End If
    End If
End Sub


Private Sub chkLaunchGameFirst_Click()
    If chkLaunchGameFirst.value = True Then
        SetLaunchGameFirst True
        GoTo SetFlag
    End If
    If chkLaunchGameFirst.value = False Then
        SetLaunchGameFirst False
        GoTo SetFlag
    End If
SetFlag:
    SetFileFlag True, Me
End Sub

Private Sub chkTrainerLaunchesGame_Click()
    ' Turn on or off being able to locate a game if the trainer launches it
    Select Case chkTrainerLaunchesGame.value
        Case vbChecked
            SetTrainerLaunchesGame True
            Me.txtGamePath.Enabled = False
            Me.txtGameSwitch.Enabled = False
            Me.cmdBrowseGame.Enabled = False
            Me.mnuProjectBrowseGame.Enabled = False
            Me.mnuProjrectSwitchGame.Enabled = False
            Me.picXPShim2.Enabled = False
            Me.fraGameData.Enabled = False
            Me.lblGameSwitch.Enabled = False
            Me.txtGamePath.BackColor = vbButtonFace
            Me.txtGameSwitch.BackColor = vbButtonFace
            GoTo SetFlag
        Case vbUnchecked
            SetTrainerLaunchesGame False
            Me.txtGamePath.Enabled = True
            Me.txtGameSwitch.Enabled = True
            Me.cmdBrowseGame.Enabled = True
            Me.mnuProjectBrowseGame.Enabled = True
            Me.mnuProjrectSwitchGame.Enabled = True
            Me.picXPShim2.Enabled = True
            Me.fraGameData.Enabled = True
            Me.lblGameSwitch.Enabled = True
            Me.txtGamePath.BackColor = vbWindowBackground
            Me.txtGameSwitch.BackColor = vbWindowBackground
            GoTo SetFlag
    End Select
SetFlag:
    SetFileFlag True, Me
End Sub

Private Sub cmdBrowseGame_Click()
    On Error GoTo Trap
    Dim winInfo As osInfo
    With Me.cdgStoreFile
        .CancelError = True
        .Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
        .DefaultExt = "exe"
        .DialogTitle = "Locate Game"
        .Flags = cdlOFNFileMustExist
        .InitDir = Environ("ProgramFiles") ' Default to %PROGRAM_FILES% since most games would be there (unlike SpecialFolder, this works on Win95/98)
        .ShowOpen
        Me.txtGamePath.Text = .FileName
        SetGamePath (.FileName)
    End With
    validateFile txtGamePath
    SetFileFlag True, Me
    Exit Sub
Trap:
    'User clicked cancel
    Exit Sub
End Sub

Private Sub cmdBrowseTrainer_Click()
    On Error GoTo Trap
    With Me.cdgStoreFile
        .CancelError = True
        .InitDir = FGetSpecialFolder(CSIDL_DOCUMENTS) '%MY_DOCUMENTS%
        .Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
        .DefaultExt = "exe"
        .DialogTitle = "Locate Trainer"
        .Flags = cdlOFNFileMustExist
        .ShowOpen
        Me.txtTrainerPath.Text = .FileName
        SetTrainerPath (.FileName)
    End With
    validateFile txtTrainerPath
    SetFileFlag True, Me
    Exit Sub
Trap:
    'User clicked cancel
    Exit Sub
End Sub

Private Sub cmdLaunch_Click()
    doLaunch
End Sub

Private Function DisplaySaveAs() As String
    On Error GoTo Trapper:
    With Me.cdgStoreFile
        .CancelError = True
        .DialogTitle = "Save StoreFile"
        .Filter = "StoreFile (*.stf)|*.stf"
        .InitDir = GetStoreFileFolder()
        .DefaultExt = "stf"
        .ShowSave
        DisplaySaveAs = .FileName
    End With
    Exit Function
Trapper:
    If Err.Number = 32755 Then 'Cancel
        DisplaySaveAs = "32755"
        Exit Function
    End If
End Function


Private Sub mnuFileExit_Click()
    Unload Me
End Sub

Private Sub mnuFileNew_Click()
    createNewStoreFile
End Sub

Private Sub mnuFileOpen_Click()
    On Error GoTo Trapper
    With Me.cdgStoreFile
        .CancelError = True
        .DialogTitle = "Open StoreFile"
        .DefaultExt = "stf"
        .Filter = "StoreTrainer File (*.stf)|*.stf"
        .InitDir = GetStoreFileFolder()
        .Flags = cdlOFNFileMustExist
        .ShowOpen
        
        ' Reads STF data into StoreTrainer
        OpenStoreFile .FileName
        
        ' Brings StoreData into the form
        Me.chkLaunchGameFirst.value = GetLaunchGameFirst
        Me.chkTrainerLaunchesGame.value = GetTrainerLaunchesGame
        Me.txtGamePath.Text = GetGamePath
        Me.txtGameSwitch.Text = GetGameSwitch
        Me.txtTrainerPath.Text = GetTrainerPath
        Me.txtTrainerSwitch.Text = GetTrainerSwitch
        
        ' Sets our form name
        SetFileName .FileName
        SetFileFlag False, Me
    End With
    Exit Sub
Trapper:
    ' Assume CancelError
    Exit Sub
End Sub

Private Sub mnuFileSave_Click()
    ' Bring up Save As if needed
    Dim strFileName As String
    strFileName = GetFileName()
    If LCase$(strFileName = "untitled") Then
        mnuFileSaveAs_Click
    End If
    
    ' Write file to disk
    Dim iRet As Integer
    iRet = SaveStoreFile(Me, GetFileName)
    
    ' iRet is any error number the disk writing routine returns
    If iRet = 0 Then Exit Sub
    If iRet <> 0 Then
        Select Case iRet
            Case 32755: 'Cancel Button somewhere
                Exit Sub
            Case Else
                MsgBox "Error " & iRet
        End Select
    End If
End Sub

Private Sub mnuFileSaveAs_Click()
    ' Brings up a Save As dialog
    Dim strFile As String
    strFile = DisplaySaveAs()
    If strFile = "32755" Then
        Exit Sub
    End If
    
    ' Continues with writing file to disk here
    SetFileName (strFile)
    mnuFileSave_Click
        
End Sub

Private Sub mnuHelpAbout_Click()
    AboutStoreTrainer
End Sub

Private Sub mnuProjectBrowseGame_Click()
    cmdBrowseGame_Click
End Sub

Private Sub mnuProjectBrowseTrainer_Click()
    cmdBrowseTrainer_Click
End Sub

Private Sub mnuProjectLaunch_Click()
    doLaunch
End Sub

Private Sub mnuProjectSwitchTrainer_Click()
    On Error GoTo Done 'We don't need to handle any errors
    Dim strTrainerSwitch As String
    strTrainerSwitch = InputBox$("Please enter a trainer switch", "Input Trainer Switch")
    If strTrainerSwitch <> "" Then
        txtTrainerSwitch.Text = strTrainerSwitch
        SetTrainerSwitch (strTrainerSwitch)
        SetFileFlag True, Me
    End If
Done:
    Exit Sub
End Sub

Private Sub mnuProjrectSwitchGame_Click()
    On Error GoTo Done 'We don't need to handle any errors
    Dim strGameSwitch As String
    strGameSwitch = InputBox$("Please enter a game switch", "Input Game Switch")
    If strGameSwitch <> "" Then
        txtGameSwitch.Text = strGameSwitch
        SetGameSwitch (strGameSwitch)
        SetFileFlag True, Me
    End If
Done:
    Exit Sub
End Sub

Private Sub txtGamePath_Change()
    SetFileFlag True, Me
End Sub

Private Sub txtGamePath_GotFocus()
    validateFile txtGamePath
End Sub

Private Sub txtGamePath_LostFocus()
    validateFile txtGamePath
End Sub

Private Sub txtGameSwitch_Change()
    SetGameSwitch txtGameSwitch.Text
    SetFileFlag True, Me
End Sub

Private Sub txtTrainerPath_Change()
    SetFileFlag True, Me
End Sub

Private Sub txtTrainerPath_GotFocus()
    validateFile txtTrainerPath
End Sub

Private Sub txtTrainerPath_LostFocus()
    validateFile txtTrainerPath
End Sub

Private Sub txtTrainerSwitch_Change()
    SetTrainerSwitch txtTrainerSwitch.Text
    SetFileFlag True, Me
End Sub

Private Sub validateFile(txt As TextBox)
    If txt.Text = "" Then Exit Sub
    
    If DoesFileExist(txt.Text) = False Then
        txt.BackColor = vbRed
        MsgBox "The file " & Chr$(34) & txtGamePath.Text & Chr$(34) & " does not exist!", vbExclamation, App.Title
    Else
        txt.BackColor = vbWindowBackground
    End If
End Sub
Private Sub createNewStoreFile()
    Dim iMSG As VbMsgBoxResult
    If GetFileFlag = True Then
        ' Save first?
        iMSG = MsgBox("Would you like to save your work before creating a new StoreFile?", vbYesNoCancel + vbQuestion, App.Title)
        If iMSG = vbCancel Then Exit Sub
        If iMSG = vbYes Then
            mnuFileSave_Click
            GoTo CreateNewFile
        End If
        If iMSG = vbNo Then GoTo CreateNewFile
    End If
    
    If GetFileFlag = False Then
        iMSG = MsgBox("Close " & GetFileName(True) & " and create a new StoreFile?", vbYesNo + vbQuestion, App.Title)
        If iMSG = vbNo Then Exit Sub
        If iMSG = vbYes Then GoTo CreateNewFile
    End If
    
    ' Done the formalities.  Create our new StoreFile
CreateNewFile:
    NewStoreFile
    Me.chkLaunchGameFirst.value = GetLaunchGameFirst
    Me.chkTrainerLaunchesGame.value = GetTrainerLaunchesGame
    Me.txtGamePath.Text = GetGamePath
    Me.txtGameSwitch.Text = GetGameSwitch
    Me.txtTrainerPath.Text = GetTrainerPath
    Me.txtTrainerSwitch = GetTrainerSwitch
    SetFileFlag False, Me
End Sub

Private Sub doLaunch()
    Dim iLaunch As Integer
    iLaunch = Launch()
    If iLaunch <> 0 Then
        Select Case iLaunch
            Case 1 'File not saved
                MsgBox "Please save your work before launching the StoreFile!", vbExclamation, App.Title
                Exit Sub
            Case 2 ' Bad game path
                MsgBox "The game path you have doesn't exist!", vbExclamation, App.Title
                Exit Sub
            Case 3 'Bad trainer path
                MsgBox "The trainer path you have doesn't exist!", vbExclamation, App.Title
                Exit Sub
        End Select
    End If
End Sub
