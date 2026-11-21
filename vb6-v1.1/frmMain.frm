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
      TabIndex        =   18
      Top             =   4320
      Width           =   1095
   End
   Begin VB.Frame fraTrainerData 
      Caption         =   "Trainer Data"
      Height          =   1335
      Left            =   120
      TabIndex        =   12
      Top             =   2880
      Width           =   6375
      Begin VB.TextBox txtTrainerPath 
         Height          =   285
         Left            =   120
         TabIndex        =   14
         Top             =   240
         Width           =   6135
      End
      Begin VB.PictureBox picXPShim3 
         BorderStyle     =   0  'None
         Height          =   975
         Left            =   120
         ScaleHeight     =   975
         ScaleWidth      =   6135
         TabIndex        =   13
         Top             =   240
         Width           =   6135
         Begin VB.CommandButton cmdBrowseTrainer 
            Caption         =   "Browse..."
            Height          =   375
            Left            =   4680
            TabIndex        =   17
            Top             =   360
            Width           =   1335
         End
         Begin VB.TextBox txtTrainerSwitch 
            Height          =   285
            Left            =   720
            TabIndex        =   16
            Top             =   360
            Width           =   3855
         End
         Begin VB.Label lblTrainerSwitch 
            AutoSize        =   -1  'True
            Caption         =   "Switch:"
            Height          =   195
            Left            =   0
            TabIndex        =   15
            Top             =   360
            Width           =   525
         End
      End
   End
   Begin VB.Frame fraGameData 
      Caption         =   "Game Data"
      Height          =   1215
      Left            =   120
      TabIndex        =   6
      Top             =   1560
      Width           =   6375
      Begin VB.PictureBox picXPShim2 
         BorderStyle     =   0  'None
         Height          =   855
         Left            =   120
         ScaleHeight     =   855
         ScaleWidth      =   6135
         TabIndex        =   7
         Top             =   240
         Width           =   6135
         Begin VB.CommandButton cmdBrowseGame 
            Caption         =   "Browse..."
            Height          =   375
            Left            =   4680
            TabIndex        =   11
            Top             =   360
            Width           =   1335
         End
         Begin VB.TextBox txtGameSwitch 
            Height          =   285
            Left            =   720
            TabIndex        =   10
            Top             =   360
            Width           =   3855
         End
         Begin VB.TextBox txtGamePath 
            Height          =   285
            Left            =   0
            TabIndex        =   8
            Top             =   0
            Width           =   6135
         End
         Begin VB.Label lblGameSwitch 
            AutoSize        =   -1  'True
            Caption         =   "Switch:"
            Height          =   195
            Left            =   0
            TabIndex        =   9
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
         Begin VB.TextBox txtLaunchDelay 
            Height          =   285
            Left            =   5160
            MaxLength       =   2
            TabIndex        =   5
            Text            =   "0"
            Top             =   345
            Width           =   735
         End
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
         Begin VB.Label lblLaunchDelay 
            AutoSize        =   -1  'True
            Caption         =   "Delay (0-30s):"
            Height          =   195
            Left            =   3960
            TabIndex        =   4
            Top             =   390
            Width           =   1035
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
      Begin VB.Menu mnuFileDashRecent 
         Caption         =   "-"
      End
      Begin VB.Menu mnuFileRecent1 
         Caption         =   "(No Recent StoreFiles)"
         Enabled         =   0   'False
      End
      Begin VB.Menu mnuFileRecent2 
         Caption         =   ""
         Visible         =   0   'False
      End
      Begin VB.Menu mnuFileRecent3 
         Caption         =   ""
         Visible         =   0   'False
      End
      Begin VB.Menu mnuFileRecent4 
         Caption         =   ""
         Visible         =   0   'False
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
      Begin VB.Menu mnuHelpUpdate 
         Caption         =   "Check for &Updates..."
      End
      Begin VB.Menu mnuHelpDash1 
         Caption         =   "-"
      End
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

' ==============================================================================
' FILE: frmMain.frm
' PROJECT: RaggieSoft StoreTrainer v1.1 (Classic Visual Basic 6.0 Edition)
' LICENSE: MIT License (Code) / CC BY-SA 4.0 (Documentation)
'
' PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
'   What is this file?
'     In Visual Basic 6.0, a '.frm' file combines two sections in plain text:
'       1. Lines 1–247: The visual window layout (measured in VB6 'Twips', where
'          15 Twips = 1 standard 96-DPI pixel; notice the 'picXPShim' PictureBoxes
'          inside each Frame — a classic VB6 trick preventing black radio/checkbox
'          artifacts when Windows XP Visual Styles are enabled!).
'       2. Everything below 'Option Explicit': The event-driven VB6 code-behind.
'
'   Upgrades & Bug Fixes in StoreTrainer v1.1:
'     - Enforces mutual exclusivity between 'Trainer Launches Game' and 'Launch
'       Game First' (fulfilling the 2018 developer roadmap note in README-dev.txt).
'     - Adds 'Delay (0-30s)' ('txtLaunchDelay') and 'File -> Recent' MRU items.
'     - Fixes 'mnuFileSave_Click' ('If LCase$(strFileName) = "untitled"') and
'       cancels cleanly if the user cancels the Save As dialog.
'     - Fixes 'validateFile' to report 'txt.Text' (instead of 'txtGamePath.Text')
'       and aligns Launch error codes 2 (missing trainer) & 3 (missing game).
' ==============================================================================

Private m_bPopulatingForm As Boolean

Private Sub Form_Load()
    SetFileName "untitled"
    SetFileFlag False, Me
    RefreshRecentFilesMenu
    ' Disable Check for Updates automatically on vintage offline OSes (Win95–Win8.1)
    ' or when the external modern updater binary is absent!
    Me.mnuHelpUpdate.Enabled = (FindModernUpdaterPath() <> "")
End Sub

Private Sub Form_Unload(Cancel As Integer)
    ReleaseAppMutex
End Sub

Private Sub Form_QueryUnload(Cancel As Integer, UnloadMode As Integer)
    Dim iMSG As VbMsgBoxResult

    If GetLaunchingSTF = True Then
        Cancel = False
        Exit Sub
    End If

    If GetFileFlag() = False Then
        iMSG = MsgBox("Are you sure you want to exit " & App.Title & "?", vbQuestion + vbYesNo, App.ProductName)
        Cancel = (iMSG = vbNo)
    Else
        iMSG = MsgBox("Would you like to save your work before exiting?", vbQuestion + vbYesNoCancel, App.ProductName)
        If iMSG = vbYes Then
            If LCase$(GetFileName()) = "untitled" Then
                Dim strSaveFile As String
                strSaveFile = DisplaySaveAs()
                If strSaveFile = "32755" Then
                    Cancel = True
                    Exit Sub
                End If
                SetFileName strSaveFile
            End If
            SaveStoreFile Me, GetFileName()
            Cancel = False
        ElseIf iMSG = vbNo Then
            Cancel = False
        Else
            Cancel = True
        End If
    End If
End Sub

' Public helper called by File -> Open, File -> Recent, and Sub Main ('/e' switch)
Public Sub LoadStoreFileIntoForm(ByVal strPath As String)
    Dim iRet As Integer
    iRet = OpenStoreFile(strPath)
    If iRet = 53 Then
        MsgBox "The StoreFile could not be found:" & vbCrLf & strPath, vbExclamation, App.ProductName
        Exit Sub
    ElseIf iRet = 1 Then
        MsgBox "This StoreFile requires a newer version of " & App.ProductName & ".", vbExclamation, App.ProductName
        Exit Sub
    ElseIf iRet <> 0 Then
        MsgBox "Unable to open StoreFile (Code " & iRet & ").", vbExclamation, App.ProductName
        Exit Sub
    End If

    m_bPopulatingForm = True
    Me.chkTrainerLaunchesGame.Value = IIf(GetTrainerLaunchesGame(), vbChecked, vbUnchecked)
    Me.chkLaunchGameFirst.Value = IIf(GetLaunchGameFirst() And Not GetTrainerLaunchesGame(), vbChecked, vbUnchecked)
    Me.txtLaunchDelay.Text = CStr(GetLaunchDelaySeconds())
    Me.txtGamePath.Text = GetGamePath()
    Me.txtGameSwitch.Text = GetGameSwitch()
    Me.txtTrainerPath.Text = GetTrainerPath()
    Me.txtTrainerSwitch.Text = GetTrainerSwitch()
    ApplyTrainerLaunchesGameUIState
    m_bPopulatingForm = False

    SetFileName strPath
    AddRecentStoreFile strPath
    RefreshRecentFilesMenu
    SetFileFlag False, Me
End Sub

Public Sub RefreshRecentFilesMenu()
    Dim s1 As String, s2 As String, s3 As String, s4 As String
    s1 = GetRecentStoreFile(1)
    s2 = GetRecentStoreFile(2)
    s3 = GetRecentStoreFile(3)
    s4 = GetRecentStoreFile(4)

    If s1 = "" Then
        mnuFileRecent1.Caption = "(No Recent StoreFiles)"
        mnuFileRecent1.Enabled = False
        mnuFileRecent1.Tag = ""
    Else
        mnuFileRecent1.Caption = "&1 " & s1
        mnuFileRecent1.Enabled = True
        mnuFileRecent1.Tag = s1
    End If

    SetupOptionalRecentMenu mnuFileRecent2, 2, s2
    SetupOptionalRecentMenu mnuFileRecent3, 3, s3
    SetupOptionalRecentMenu mnuFileRecent4, 4, s4
End Sub

Private Sub SetupOptionalRecentMenu(mnuItem As Menu, ByVal idx As Integer, ByVal sPath As String)
    If sPath = "" Then
        mnuItem.Visible = False
        mnuItem.Tag = ""
    Else
        mnuItem.Caption = "&" & CStr(idx) & " " & sPath
        mnuItem.Tag = sPath
        mnuItem.Visible = True
    End If
End Sub

Private Sub mnuFileRecent1_Click()
    OpenRecentFromTag mnuFileRecent1.Tag
End Sub

Private Sub mnuFileRecent2_Click()
    OpenRecentFromTag mnuFileRecent2.Tag
End Sub

Private Sub mnuFileRecent3_Click()
    OpenRecentFromTag mnuFileRecent3.Tag
End Sub

Private Sub mnuFileRecent4_Click()
    OpenRecentFromTag mnuFileRecent4.Tag
End Sub

Private Sub OpenRecentFromTag(ByVal sPath As String)
    If sPath = "" Then Exit Sub
    If GetFileFlag() = True Then
        Dim iMSG As VbMsgBoxResult
        iMSG = MsgBox("Save changes to " & GetFileName(True) & " before opening another StoreFile?", vbYesNoCancel + vbQuestion, App.ProductName)
        If iMSG = vbCancel Then Exit Sub
        If iMSG = vbYes Then mnuFileSave_Click
    End If
    LoadStoreFileIntoForm sPath
End Sub

Private Sub chkLaunchGameFirst_Click()
    If m_bPopulatingForm Then Exit Sub
    SetLaunchGameFirst (chkLaunchGameFirst.Value = vbChecked)
    SetFileFlag True, Me
End Sub

Private Sub ApplyTrainerLaunchesGameUIState()
    Dim bTrainerStartsGame As Boolean
    bTrainerStartsGame = (chkTrainerLaunchesGame.Value = vbChecked)
    SetTrainerLaunchesGame bTrainerStartsGame

    If bTrainerStartsGame Then
        Me.chkLaunchGameFirst.Value = vbUnchecked
        Me.chkLaunchGameFirst.Enabled = False
        Me.txtLaunchDelay.Enabled = False
        Me.lblLaunchDelay.Enabled = False
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
        Me.txtLaunchDelay.BackColor = vbButtonFace
    Else
        Me.chkLaunchGameFirst.Enabled = True
        Me.txtLaunchDelay.Enabled = True
        Me.lblLaunchDelay.Enabled = True
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
        Me.txtLaunchDelay.BackColor = vbWindowBackground
    End If
End Sub

Private Sub chkTrainerLaunchesGame_Click()
    If m_bPopulatingForm Then Exit Sub
    ApplyTrainerLaunchesGameUIState
    SetFileFlag True, Me
End Sub

Private Sub txtLaunchDelay_Change()
    If m_bPopulatingForm Then Exit Sub
    SetLaunchDelaySeconds CLng(Val(txtLaunchDelay.Text))
    SetFileFlag True, Me
End Sub

Private Sub cmdBrowseGame_Click()
    On Error GoTo Trap
    With Me.cdgStoreFile
        .CancelError = True
        .Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
        .DefaultExt = "exe"
        .DialogTitle = "Locate Game"
        .Flags = cdlOFNFileMustExist
        .InitDir = Environ("ProgramFiles")
        .ShowOpen
        Me.txtGamePath.Text = .FileName
        SetGamePath .FileName
    End With
    validateFile txtGamePath
    SetFileFlag True, Me
    Exit Sub
Trap:
    Exit Sub
End Sub

Private Sub cmdBrowseTrainer_Click()
    On Error GoTo Trap
    With Me.cdgStoreFile
        .CancelError = True
        .InitDir = FGetSpecialFolder(CSIDL_DOCUMENTS)
        .Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
        .DefaultExt = "exe"
        .DialogTitle = "Locate Trainer"
        .Flags = cdlOFNFileMustExist
        .ShowOpen
        Me.txtTrainerPath.Text = .FileName
        SetTrainerPath .FileName
    End With
    validateFile txtTrainerPath
    SetFileFlag True, Me
    Exit Sub
Trap:
    Exit Sub
End Sub

Private Sub cmdLaunch_Click()
    doLaunch
End Sub

Private Function DisplaySaveAs() As String
    On Error GoTo Trapper
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
    If Err.Number = 32755 Then
        DisplaySaveAs = "32755"
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
        LoadStoreFileIntoForm .FileName
    End With
    Exit Sub
Trapper:
    Exit Sub
End Sub

Private Sub mnuFileSave_Click()
    Dim strCurFile As String
    strCurFile = GetFileName()
    If LCase$(strCurFile) = "untitled" Then
        mnuFileSaveAs_Click
        Exit Sub
    End If

    Dim iRet As Integer
    iRet = SaveStoreFile(Me, GetFileName())
    RefreshRecentFilesMenu

    If iRet <> 0 And iRet <> 32755 Then
        MsgBox "Error saving StoreFile: " & iRet, vbExclamation, App.ProductName
    End If
End Sub

Private Sub mnuFileSaveAs_Click()
    Dim strFile As String
    strFile = DisplaySaveAs()
    If strFile = "32755" Or strFile = "" Then
        Exit Sub
    End If

    SetFileName strFile
    SaveStoreFile Me, strFile
    RefreshRecentFilesMenu
End Sub

Private Sub mnuHelpUpdate_Click()
    CheckForUpdatesViaExternalBinary
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
    On Error GoTo Done
    Dim strTrainerSwitch As String
    strTrainerSwitch = InputBox$("Please enter a trainer switch", "Input Trainer Switch", GetTrainerSwitch())
    If strTrainerSwitch <> "" Then
        txtTrainerSwitch.Text = strTrainerSwitch
        SetTrainerSwitch strTrainerSwitch
        SetFileFlag True, Me
    End If
Done:
End Sub

Private Sub mnuProjrectSwitchGame_Click()
    On Error GoTo Done
    Dim strGameSwitch As String
    strGameSwitch = InputBox$("Please enter a game switch", "Input Game Switch", GetGameSwitch())
    If strGameSwitch <> "" Then
        txtGameSwitch.Text = strGameSwitch
        SetGameSwitch strGameSwitch
        SetFileFlag True, Me
    End If
Done:
End Sub

Private Sub txtGamePath_Change()
    If m_bPopulatingForm Then Exit Sub
    SetGamePath txtGamePath.Text
    SetFileFlag True, Me
End Sub

Private Sub txtGamePath_LostFocus()
    validateFile txtGamePath
End Sub

Private Sub txtGameSwitch_Change()
    If m_bPopulatingForm Then Exit Sub
    SetGameSwitch txtGameSwitch.Text
    SetFileFlag True, Me
End Sub

Private Sub txtTrainerPath_Change()
    If m_bPopulatingForm Then Exit Sub
    SetTrainerPath txtTrainerPath.Text
    SetFileFlag True, Me
End Sub

Private Sub txtTrainerPath_LostFocus()
    validateFile txtTrainerPath
End Sub

Private Sub txtTrainerSwitch_Change()
    If m_bPopulatingForm Then Exit Sub
    SetTrainerSwitch txtTrainerSwitch.Text
    SetFileFlag True, Me
End Sub

Private Sub validateFile(txt As TextBox)
    If Trim$(txt.Text) = "" Then
        txt.BackColor = vbWindowBackground
        Exit Sub
    End If

    If DoesFileExist(txt.Text) = False Then
        txt.BackColor = vbRed
        MsgBox "The file " & Chr$(34) & txt.Text & Chr$(34) & " does not exist!", vbExclamation, App.Title
    Else
        txt.BackColor = vbWindowBackground
    End If
End Sub

Private Sub createNewStoreFile()
    Dim iMSG As VbMsgBoxResult
    If GetFileFlag = True Then
        iMSG = MsgBox("Would you like to save your work before creating a new StoreFile?", vbYesNoCancel + vbQuestion, App.Title)
        If iMSG = vbCancel Then Exit Sub
        If iMSG = vbYes Then mnuFileSave_Click
    Else
        iMSG = MsgBox("Close " & GetFileName(True) & " and create a new StoreFile?", vbYesNo + vbQuestion, App.Title)
        If iMSG = vbNo Then Exit Sub
    End If

    m_bPopulatingForm = True
    NewStoreFile
    Me.chkTrainerLaunchesGame.Value = vbUnchecked
    Me.chkLaunchGameFirst.Value = vbUnchecked
    Me.txtLaunchDelay.Text = "0"
    Me.txtGamePath.Text = ""
    Me.txtGameSwitch.Text = ""
    Me.txtTrainerPath.Text = ""
    Me.txtTrainerSwitch.Text = ""
    ApplyTrainerLaunchesGameUIState
    m_bPopulatingForm = False
    SetFileFlag False, Me
End Sub

Private Sub doLaunch()
    Dim iLaunch As Integer
    iLaunch = Launch()
    If iLaunch <> 0 Then
        Select Case iLaunch
            Case 1
                MsgBox "Please save your StoreFile before launching!", vbExclamation, App.Title
            Case 2
                MsgBox "The trainer file path does not exist!", vbExclamation, App.Title
            Case 3
                MsgBox "The game file path does not exist!", vbExclamation, App.Title
            Case Else
                MsgBox "Unable to start the game or trainer (Error " & iLaunch & ").", vbExclamation, App.Title
        End Select
    End If
End Sub
