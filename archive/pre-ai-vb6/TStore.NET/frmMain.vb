Option Strict Off
Option Explicit On
Friend Class frmMain
	Inherits System.Windows.Forms.Form
#Region "Windows Form Designer generated code "
	Public Sub New()
		MyBase.New()
		If m_vb6FormDefInstance Is Nothing Then
			If m_InitializingDefInstance Then
				m_vb6FormDefInstance = Me
			Else
				Try 
					'For the start-up form, the first instance created is the default instance.
					If System.Reflection.Assembly.GetExecutingAssembly.EntryPoint.DeclaringType Is Me.GetType Then
						m_vb6FormDefInstance = Me
					End If
				Catch
				End Try
			End If
		End If
		'This call is required by the Windows Form Designer.
		InitializeComponent()
	End Sub
	'Form overrides dispose to clean up the component list.
	Protected Overloads Overrides Sub Dispose(ByVal Disposing As Boolean)
		If Disposing Then
			If Not components Is Nothing Then
				components.Dispose()
			End If
		End If
		MyBase.Dispose(Disposing)
	End Sub
	'Required by the Windows Form Designer
	Private components As System.ComponentModel.IContainer
	Public ToolTip1 As System.Windows.Forms.ToolTip
	Public WithEvents cmdLaunch As System.Windows.Forms.Button
	Public WithEvents txtTrainerPath As System.Windows.Forms.TextBox
	Public WithEvents cmdBrowseTrainer As System.Windows.Forms.Button
	Public WithEvents txtTrainerSwitch As System.Windows.Forms.TextBox
	Public WithEvents lblTrainerSwitch As System.Windows.Forms.Label
	Public WithEvents picXPShim3 As System.Windows.Forms.Panel
	Public WithEvents fraTrainerData As System.Windows.Forms.GroupBox
	Public WithEvents cmdBrowseGame As System.Windows.Forms.Button
	Public WithEvents txtGameSwitch As System.Windows.Forms.TextBox
	Public WithEvents txtGamePath As System.Windows.Forms.TextBox
	Public WithEvents lblGameSwitch As System.Windows.Forms.Label
	Public WithEvents picXPShim2 As System.Windows.Forms.Panel
	Public WithEvents fraGameData As System.Windows.Forms.GroupBox
	Public WithEvents chkLaunchGameFirst As System.Windows.Forms.CheckBox
	Public WithEvents chkTrainerLaunchesGame As System.Windows.Forms.CheckBox
	Public WithEvents picXPShim1 As System.Windows.Forms.Panel
	Public WithEvents fraStoreFileDetails As System.Windows.Forms.GroupBox
	Public WithEvents cdgStoreFile As AxMSComDlg.AxCommonDialog
	Public WithEvents mnuFileNew As System.Windows.Forms.MenuItem
	Public WithEvents mnuFileOpen As System.Windows.Forms.MenuItem
	Public WithEvents mnuFileDash1 As System.Windows.Forms.MenuItem
	Public WithEvents mnuFileSave As System.Windows.Forms.MenuItem
	Public WithEvents mnuFileSaveAs As System.Windows.Forms.MenuItem
	Public WithEvents mnuFileDash2 As System.Windows.Forms.MenuItem
	Public WithEvents mnuFileExit As System.Windows.Forms.MenuItem
	Public WithEvents mnuFile As System.Windows.Forms.MenuItem
	Public WithEvents mnuProjectBrowseGame As System.Windows.Forms.MenuItem
	Public WithEvents mnuProjectBrowseTrainer As System.Windows.Forms.MenuItem
	Public WithEvents mnuProjectBrowse As System.Windows.Forms.MenuItem
	Public WithEvents mnuProjectDash1 As System.Windows.Forms.MenuItem
	Public WithEvents mnuProjrectSwitchGame As System.Windows.Forms.MenuItem
	Public WithEvents mnuProjectSwitchTrainer As System.Windows.Forms.MenuItem
	Public WithEvents mnuProjectSwitch As System.Windows.Forms.MenuItem
	Public WithEvents mnuProjectDash2 As System.Windows.Forms.MenuItem
	Public WithEvents mnuProjectLaunch As System.Windows.Forms.MenuItem
	Public WithEvents mnuProject As System.Windows.Forms.MenuItem
	Public WithEvents mnuHelpAbout As System.Windows.Forms.MenuItem
	Public WithEvents mnuHelpCheckForUpdate As System.Windows.Forms.MenuItem
	Public WithEvents mnuHelp As System.Windows.Forms.MenuItem
	Public MainMenu1 As System.Windows.Forms.MainMenu
	'NOTE: The following procedure is required by the Windows Form Designer
	'It can be modified using the Windows Form Designer.
	'Do not modify it using the code editor.
	<System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
		Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(frmMain))
		Me.components = New System.ComponentModel.Container()
		Me.ToolTip1 = New System.Windows.Forms.ToolTip(components)
		Me.ToolTip1.Active = True
		Me.cmdLaunch = New System.Windows.Forms.Button
		Me.fraTrainerData = New System.Windows.Forms.GroupBox
		Me.txtTrainerPath = New System.Windows.Forms.TextBox
		Me.picXPShim3 = New System.Windows.Forms.Panel
		Me.cmdBrowseTrainer = New System.Windows.Forms.Button
		Me.txtTrainerSwitch = New System.Windows.Forms.TextBox
		Me.lblTrainerSwitch = New System.Windows.Forms.Label
		Me.fraGameData = New System.Windows.Forms.GroupBox
		Me.picXPShim2 = New System.Windows.Forms.Panel
		Me.cmdBrowseGame = New System.Windows.Forms.Button
		Me.txtGameSwitch = New System.Windows.Forms.TextBox
		Me.txtGamePath = New System.Windows.Forms.TextBox
		Me.lblGameSwitch = New System.Windows.Forms.Label
		Me.fraStoreFileDetails = New System.Windows.Forms.GroupBox
		Me.picXPShim1 = New System.Windows.Forms.Panel
		Me.chkLaunchGameFirst = New System.Windows.Forms.CheckBox
		Me.chkTrainerLaunchesGame = New System.Windows.Forms.CheckBox
		Me.cdgStoreFile = New AxMSComDlg.AxCommonDialog
		Me.MainMenu1 = New System.Windows.Forms.MainMenu
		Me.mnuFile = New System.Windows.Forms.MenuItem
		Me.mnuFileNew = New System.Windows.Forms.MenuItem
		Me.mnuFileOpen = New System.Windows.Forms.MenuItem
		Me.mnuFileDash1 = New System.Windows.Forms.MenuItem
		Me.mnuFileSave = New System.Windows.Forms.MenuItem
		Me.mnuFileSaveAs = New System.Windows.Forms.MenuItem
		Me.mnuFileDash2 = New System.Windows.Forms.MenuItem
		Me.mnuFileExit = New System.Windows.Forms.MenuItem
		Me.mnuProject = New System.Windows.Forms.MenuItem
		Me.mnuProjectBrowse = New System.Windows.Forms.MenuItem
		Me.mnuProjectBrowseGame = New System.Windows.Forms.MenuItem
		Me.mnuProjectBrowseTrainer = New System.Windows.Forms.MenuItem
		Me.mnuProjectDash1 = New System.Windows.Forms.MenuItem
		Me.mnuProjectSwitch = New System.Windows.Forms.MenuItem
		Me.mnuProjrectSwitchGame = New System.Windows.Forms.MenuItem
		Me.mnuProjectSwitchTrainer = New System.Windows.Forms.MenuItem
		Me.mnuProjectDash2 = New System.Windows.Forms.MenuItem
		Me.mnuProjectLaunch = New System.Windows.Forms.MenuItem
		Me.mnuHelp = New System.Windows.Forms.MenuItem
		Me.mnuHelpAbout = New System.Windows.Forms.MenuItem
		Me.mnuHelpCheckForUpdate = New System.Windows.Forms.MenuItem
		CType(Me.cdgStoreFile, System.ComponentModel.ISupportInitialize).BeginInit()
		Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
		Me.Text = "[UNTITLED] - RaggieSoft StoreTrainer"
		Me.ClientSize = New System.Drawing.Size(441, 327)
		Me.Location = New System.Drawing.Point(3, 41)
		Me.Icon = CType(resources.GetObject("frmMain.Icon"), System.Drawing.Icon)
		Me.MaximizeBox = False
		Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
		Me.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
		Me.BackColor = System.Drawing.SystemColors.Control
		Me.ControlBox = True
		Me.Enabled = True
		Me.KeyPreview = False
		Me.MinimizeBox = True
		Me.Cursor = System.Windows.Forms.Cursors.Default
		Me.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.ShowInTaskbar = True
		Me.HelpButton = False
		Me.WindowState = System.Windows.Forms.FormWindowState.Normal
		Me.Name = "frmMain"
		Me.cmdLaunch.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me.cmdLaunch.Text = "&Launch"
		Me.cmdLaunch.Size = New System.Drawing.Size(73, 33)
		Me.cmdLaunch.Location = New System.Drawing.Point(360, 288)
		Me.cmdLaunch.TabIndex = 16
		Me.cmdLaunch.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.cmdLaunch.BackColor = System.Drawing.SystemColors.Control
		Me.cmdLaunch.CausesValidation = True
		Me.cmdLaunch.Enabled = True
		Me.cmdLaunch.ForeColor = System.Drawing.SystemColors.ControlText
		Me.cmdLaunch.Cursor = System.Windows.Forms.Cursors.Default
		Me.cmdLaunch.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.cmdLaunch.TabStop = True
		Me.cmdLaunch.Name = "cmdLaunch"
		Me.fraTrainerData.Text = "Trainer Data"
		Me.fraTrainerData.Size = New System.Drawing.Size(425, 89)
		Me.fraTrainerData.Location = New System.Drawing.Point(8, 192)
		Me.fraTrainerData.TabIndex = 10
		Me.fraTrainerData.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.fraTrainerData.BackColor = System.Drawing.SystemColors.Control
		Me.fraTrainerData.Enabled = True
		Me.fraTrainerData.ForeColor = System.Drawing.SystemColors.ControlText
		Me.fraTrainerData.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.fraTrainerData.Visible = True
		Me.fraTrainerData.Name = "fraTrainerData"
		Me.txtTrainerPath.AutoSize = False
		Me.txtTrainerPath.Size = New System.Drawing.Size(409, 19)
		Me.txtTrainerPath.Location = New System.Drawing.Point(8, 16)
		Me.txtTrainerPath.TabIndex = 12
		Me.txtTrainerPath.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.txtTrainerPath.AcceptsReturn = True
		Me.txtTrainerPath.TextAlign = System.Windows.Forms.HorizontalAlignment.Left
		Me.txtTrainerPath.BackColor = System.Drawing.SystemColors.Window
		Me.txtTrainerPath.CausesValidation = True
		Me.txtTrainerPath.Enabled = True
		Me.txtTrainerPath.ForeColor = System.Drawing.SystemColors.WindowText
		Me.txtTrainerPath.HideSelection = True
		Me.txtTrainerPath.ReadOnly = False
		Me.txtTrainerPath.Maxlength = 0
		Me.txtTrainerPath.Cursor = System.Windows.Forms.Cursors.IBeam
		Me.txtTrainerPath.MultiLine = False
		Me.txtTrainerPath.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.txtTrainerPath.ScrollBars = System.Windows.Forms.ScrollBars.None
		Me.txtTrainerPath.TabStop = True
		Me.txtTrainerPath.Visible = True
		Me.txtTrainerPath.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.txtTrainerPath.Name = "txtTrainerPath"
		Me.picXPShim3.Size = New System.Drawing.Size(409, 65)
		Me.picXPShim3.Location = New System.Drawing.Point(8, 16)
		Me.picXPShim3.TabIndex = 11
		Me.picXPShim3.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.picXPShim3.Dock = System.Windows.Forms.DockStyle.None
		Me.picXPShim3.BackColor = System.Drawing.SystemColors.Control
		Me.picXPShim3.CausesValidation = True
		Me.picXPShim3.Enabled = True
		Me.picXPShim3.ForeColor = System.Drawing.SystemColors.ControlText
		Me.picXPShim3.Cursor = System.Windows.Forms.Cursors.Default
		Me.picXPShim3.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.picXPShim3.TabStop = True
		Me.picXPShim3.Visible = True
		Me.picXPShim3.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.picXPShim3.Name = "picXPShim3"
		Me.cmdBrowseTrainer.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me.cmdBrowseTrainer.Text = "Browse..."
		Me.cmdBrowseTrainer.Size = New System.Drawing.Size(89, 25)
		Me.cmdBrowseTrainer.Location = New System.Drawing.Point(312, 24)
		Me.cmdBrowseTrainer.TabIndex = 15
		Me.cmdBrowseTrainer.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.cmdBrowseTrainer.BackColor = System.Drawing.SystemColors.Control
		Me.cmdBrowseTrainer.CausesValidation = True
		Me.cmdBrowseTrainer.Enabled = True
		Me.cmdBrowseTrainer.ForeColor = System.Drawing.SystemColors.ControlText
		Me.cmdBrowseTrainer.Cursor = System.Windows.Forms.Cursors.Default
		Me.cmdBrowseTrainer.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.cmdBrowseTrainer.TabStop = True
		Me.cmdBrowseTrainer.Name = "cmdBrowseTrainer"
		Me.txtTrainerSwitch.AutoSize = False
		Me.txtTrainerSwitch.Size = New System.Drawing.Size(257, 19)
		Me.txtTrainerSwitch.Location = New System.Drawing.Point(48, 24)
		Me.txtTrainerSwitch.TabIndex = 14
		Me.txtTrainerSwitch.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.txtTrainerSwitch.AcceptsReturn = True
		Me.txtTrainerSwitch.TextAlign = System.Windows.Forms.HorizontalAlignment.Left
		Me.txtTrainerSwitch.BackColor = System.Drawing.SystemColors.Window
		Me.txtTrainerSwitch.CausesValidation = True
		Me.txtTrainerSwitch.Enabled = True
		Me.txtTrainerSwitch.ForeColor = System.Drawing.SystemColors.WindowText
		Me.txtTrainerSwitch.HideSelection = True
		Me.txtTrainerSwitch.ReadOnly = False
		Me.txtTrainerSwitch.Maxlength = 0
		Me.txtTrainerSwitch.Cursor = System.Windows.Forms.Cursors.IBeam
		Me.txtTrainerSwitch.MultiLine = False
		Me.txtTrainerSwitch.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.txtTrainerSwitch.ScrollBars = System.Windows.Forms.ScrollBars.None
		Me.txtTrainerSwitch.TabStop = True
		Me.txtTrainerSwitch.Visible = True
		Me.txtTrainerSwitch.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.txtTrainerSwitch.Name = "txtTrainerSwitch"
		Me.lblTrainerSwitch.Text = "Switch:"
		Me.lblTrainerSwitch.Size = New System.Drawing.Size(35, 13)
		Me.lblTrainerSwitch.Location = New System.Drawing.Point(0, 24)
		Me.lblTrainerSwitch.TabIndex = 13
		Me.lblTrainerSwitch.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.lblTrainerSwitch.TextAlign = System.Drawing.ContentAlignment.TopLeft
		Me.lblTrainerSwitch.BackColor = System.Drawing.SystemColors.Control
		Me.lblTrainerSwitch.Enabled = True
		Me.lblTrainerSwitch.ForeColor = System.Drawing.SystemColors.ControlText
		Me.lblTrainerSwitch.Cursor = System.Windows.Forms.Cursors.Default
		Me.lblTrainerSwitch.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.lblTrainerSwitch.UseMnemonic = True
		Me.lblTrainerSwitch.Visible = True
		Me.lblTrainerSwitch.AutoSize = True
		Me.lblTrainerSwitch.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.lblTrainerSwitch.Name = "lblTrainerSwitch"
		Me.fraGameData.Text = "Game Data"
		Me.fraGameData.Size = New System.Drawing.Size(425, 81)
		Me.fraGameData.Location = New System.Drawing.Point(8, 104)
		Me.fraGameData.TabIndex = 4
		Me.fraGameData.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.fraGameData.BackColor = System.Drawing.SystemColors.Control
		Me.fraGameData.Enabled = True
		Me.fraGameData.ForeColor = System.Drawing.SystemColors.ControlText
		Me.fraGameData.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.fraGameData.Visible = True
		Me.fraGameData.Name = "fraGameData"
		Me.picXPShim2.Size = New System.Drawing.Size(409, 57)
		Me.picXPShim2.Location = New System.Drawing.Point(8, 16)
		Me.picXPShim2.TabIndex = 5
		Me.picXPShim2.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.picXPShim2.Dock = System.Windows.Forms.DockStyle.None
		Me.picXPShim2.BackColor = System.Drawing.SystemColors.Control
		Me.picXPShim2.CausesValidation = True
		Me.picXPShim2.Enabled = True
		Me.picXPShim2.ForeColor = System.Drawing.SystemColors.ControlText
		Me.picXPShim2.Cursor = System.Windows.Forms.Cursors.Default
		Me.picXPShim2.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.picXPShim2.TabStop = True
		Me.picXPShim2.Visible = True
		Me.picXPShim2.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.picXPShim2.Name = "picXPShim2"
		Me.cmdBrowseGame.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me.cmdBrowseGame.Text = "Browse..."
		Me.cmdBrowseGame.Size = New System.Drawing.Size(89, 25)
		Me.cmdBrowseGame.Location = New System.Drawing.Point(312, 24)
		Me.cmdBrowseGame.TabIndex = 9
		Me.cmdBrowseGame.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.cmdBrowseGame.BackColor = System.Drawing.SystemColors.Control
		Me.cmdBrowseGame.CausesValidation = True
		Me.cmdBrowseGame.Enabled = True
		Me.cmdBrowseGame.ForeColor = System.Drawing.SystemColors.ControlText
		Me.cmdBrowseGame.Cursor = System.Windows.Forms.Cursors.Default
		Me.cmdBrowseGame.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.cmdBrowseGame.TabStop = True
		Me.cmdBrowseGame.Name = "cmdBrowseGame"
		Me.txtGameSwitch.AutoSize = False
		Me.txtGameSwitch.Size = New System.Drawing.Size(257, 19)
		Me.txtGameSwitch.Location = New System.Drawing.Point(48, 24)
		Me.txtGameSwitch.TabIndex = 8
		Me.txtGameSwitch.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.txtGameSwitch.AcceptsReturn = True
		Me.txtGameSwitch.TextAlign = System.Windows.Forms.HorizontalAlignment.Left
		Me.txtGameSwitch.BackColor = System.Drawing.SystemColors.Window
		Me.txtGameSwitch.CausesValidation = True
		Me.txtGameSwitch.Enabled = True
		Me.txtGameSwitch.ForeColor = System.Drawing.SystemColors.WindowText
		Me.txtGameSwitch.HideSelection = True
		Me.txtGameSwitch.ReadOnly = False
		Me.txtGameSwitch.Maxlength = 0
		Me.txtGameSwitch.Cursor = System.Windows.Forms.Cursors.IBeam
		Me.txtGameSwitch.MultiLine = False
		Me.txtGameSwitch.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.txtGameSwitch.ScrollBars = System.Windows.Forms.ScrollBars.None
		Me.txtGameSwitch.TabStop = True
		Me.txtGameSwitch.Visible = True
		Me.txtGameSwitch.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.txtGameSwitch.Name = "txtGameSwitch"
		Me.txtGamePath.AutoSize = False
		Me.txtGamePath.Size = New System.Drawing.Size(409, 19)
		Me.txtGamePath.Location = New System.Drawing.Point(0, 0)
		Me.txtGamePath.TabIndex = 6
		Me.txtGamePath.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.txtGamePath.AcceptsReturn = True
		Me.txtGamePath.TextAlign = System.Windows.Forms.HorizontalAlignment.Left
		Me.txtGamePath.BackColor = System.Drawing.SystemColors.Window
		Me.txtGamePath.CausesValidation = True
		Me.txtGamePath.Enabled = True
		Me.txtGamePath.ForeColor = System.Drawing.SystemColors.WindowText
		Me.txtGamePath.HideSelection = True
		Me.txtGamePath.ReadOnly = False
		Me.txtGamePath.Maxlength = 0
		Me.txtGamePath.Cursor = System.Windows.Forms.Cursors.IBeam
		Me.txtGamePath.MultiLine = False
		Me.txtGamePath.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.txtGamePath.ScrollBars = System.Windows.Forms.ScrollBars.None
		Me.txtGamePath.TabStop = True
		Me.txtGamePath.Visible = True
		Me.txtGamePath.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.txtGamePath.Name = "txtGamePath"
		Me.lblGameSwitch.Text = "Switch:"
		Me.lblGameSwitch.Size = New System.Drawing.Size(35, 13)
		Me.lblGameSwitch.Location = New System.Drawing.Point(0, 24)
		Me.lblGameSwitch.TabIndex = 7
		Me.lblGameSwitch.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.lblGameSwitch.TextAlign = System.Drawing.ContentAlignment.TopLeft
		Me.lblGameSwitch.BackColor = System.Drawing.SystemColors.Control
		Me.lblGameSwitch.Enabled = True
		Me.lblGameSwitch.ForeColor = System.Drawing.SystemColors.ControlText
		Me.lblGameSwitch.Cursor = System.Windows.Forms.Cursors.Default
		Me.lblGameSwitch.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.lblGameSwitch.UseMnemonic = True
		Me.lblGameSwitch.Visible = True
		Me.lblGameSwitch.AutoSize = True
		Me.lblGameSwitch.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.lblGameSwitch.Name = "lblGameSwitch"
		Me.fraStoreFileDetails.Text = "General Data"
		Me.fraStoreFileDetails.Size = New System.Drawing.Size(425, 89)
		Me.fraStoreFileDetails.Location = New System.Drawing.Point(8, 8)
		Me.fraStoreFileDetails.TabIndex = 0
		Me.fraStoreFileDetails.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.fraStoreFileDetails.BackColor = System.Drawing.SystemColors.Control
		Me.fraStoreFileDetails.Enabled = True
		Me.fraStoreFileDetails.ForeColor = System.Drawing.SystemColors.ControlText
		Me.fraStoreFileDetails.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.fraStoreFileDetails.Visible = True
		Me.fraStoreFileDetails.Name = "fraStoreFileDetails"
		Me.picXPShim1.Size = New System.Drawing.Size(409, 65)
		Me.picXPShim1.Location = New System.Drawing.Point(8, 16)
		Me.picXPShim1.TabIndex = 1
		Me.picXPShim1.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.picXPShim1.Dock = System.Windows.Forms.DockStyle.None
		Me.picXPShim1.BackColor = System.Drawing.SystemColors.Control
		Me.picXPShim1.CausesValidation = True
		Me.picXPShim1.Enabled = True
		Me.picXPShim1.ForeColor = System.Drawing.SystemColors.ControlText
		Me.picXPShim1.Cursor = System.Windows.Forms.Cursors.Default
		Me.picXPShim1.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.picXPShim1.TabStop = True
		Me.picXPShim1.Visible = True
		Me.picXPShim1.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.picXPShim1.Name = "picXPShim1"
		Me.chkLaunchGameFirst.Text = "&Launch Game First"
		Me.chkLaunchGameFirst.Size = New System.Drawing.Size(161, 17)
		Me.chkLaunchGameFirst.Location = New System.Drawing.Point(8, 24)
		Me.chkLaunchGameFirst.TabIndex = 3
		Me.chkLaunchGameFirst.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.chkLaunchGameFirst.CheckAlign = System.Drawing.ContentAlignment.MiddleLeft
		Me.chkLaunchGameFirst.BackColor = System.Drawing.SystemColors.Control
		Me.chkLaunchGameFirst.CausesValidation = True
		Me.chkLaunchGameFirst.Enabled = True
		Me.chkLaunchGameFirst.ForeColor = System.Drawing.SystemColors.ControlText
		Me.chkLaunchGameFirst.Cursor = System.Windows.Forms.Cursors.Default
		Me.chkLaunchGameFirst.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.chkLaunchGameFirst.Appearance = System.Windows.Forms.Appearance.Normal
		Me.chkLaunchGameFirst.TabStop = True
		Me.chkLaunchGameFirst.CheckState = System.Windows.Forms.CheckState.Unchecked
		Me.chkLaunchGameFirst.Visible = True
		Me.chkLaunchGameFirst.Name = "chkLaunchGameFirst"
		Me.chkTrainerLaunchesGame.Text = "&Trainer Launches Game"
		Me.chkTrainerLaunchesGame.Size = New System.Drawing.Size(177, 17)
		Me.chkTrainerLaunchesGame.Location = New System.Drawing.Point(8, 0)
		Me.chkTrainerLaunchesGame.TabIndex = 2
		Me.chkTrainerLaunchesGame.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.chkTrainerLaunchesGame.CheckAlign = System.Drawing.ContentAlignment.MiddleLeft
		Me.chkTrainerLaunchesGame.BackColor = System.Drawing.SystemColors.Control
		Me.chkTrainerLaunchesGame.CausesValidation = True
		Me.chkTrainerLaunchesGame.Enabled = True
		Me.chkTrainerLaunchesGame.ForeColor = System.Drawing.SystemColors.ControlText
		Me.chkTrainerLaunchesGame.Cursor = System.Windows.Forms.Cursors.Default
		Me.chkTrainerLaunchesGame.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.chkTrainerLaunchesGame.Appearance = System.Windows.Forms.Appearance.Normal
		Me.chkTrainerLaunchesGame.TabStop = True
		Me.chkTrainerLaunchesGame.CheckState = System.Windows.Forms.CheckState.Unchecked
		Me.chkTrainerLaunchesGame.Visible = True
		Me.chkTrainerLaunchesGame.Name = "chkTrainerLaunchesGame"
		cdgStoreFile.OcxState = CType(resources.GetObject("cdgStoreFile.OcxState"), System.Windows.Forms.AxHost.State)
		Me.cdgStoreFile.Location = New System.Drawing.Point(8, 288)
		Me.cdgStoreFile.Name = "cdgStoreFile"
		Me.mnuFile.Text = "&File"
		Me.mnuFile.Checked = False
		Me.mnuFile.Enabled = True
		Me.mnuFile.Visible = True
		Me.mnuFile.MDIList = False
		Me.mnuFileNew.Text = "&New"
		Me.mnuFileNew.Shortcut = System.Windows.Forms.Shortcut.CtrlN
		Me.mnuFileNew.Checked = False
		Me.mnuFileNew.Enabled = True
		Me.mnuFileNew.Visible = True
		Me.mnuFileNew.MDIList = False
		Me.mnuFileOpen.Text = "&Open..."
		Me.mnuFileOpen.Shortcut = System.Windows.Forms.Shortcut.CtrlO
		Me.mnuFileOpen.Checked = False
		Me.mnuFileOpen.Enabled = True
		Me.mnuFileOpen.Visible = True
		Me.mnuFileOpen.MDIList = False
		Me.mnuFileDash1.Text = "-"
		Me.mnuFileDash1.Checked = False
		Me.mnuFileDash1.Enabled = True
		Me.mnuFileDash1.Visible = True
		Me.mnuFileDash1.MDIList = False
		Me.mnuFileSave.Text = "&Save"
		Me.mnuFileSave.Shortcut = System.Windows.Forms.Shortcut.CtrlS
		Me.mnuFileSave.Checked = False
		Me.mnuFileSave.Enabled = True
		Me.mnuFileSave.Visible = True
		Me.mnuFileSave.MDIList = False
		Me.mnuFileSaveAs.Text = "Save &As..."
		Me.mnuFileSaveAs.Checked = False
		Me.mnuFileSaveAs.Enabled = True
		Me.mnuFileSaveAs.Visible = True
		Me.mnuFileSaveAs.MDIList = False
		Me.mnuFileDash2.Text = "-"
		Me.mnuFileDash2.Checked = False
		Me.mnuFileDash2.Enabled = True
		Me.mnuFileDash2.Visible = True
		Me.mnuFileDash2.MDIList = False
		Me.mnuFileExit.Text = "E&xit"
		Me.mnuFileExit.Checked = False
		Me.mnuFileExit.Enabled = True
		Me.mnuFileExit.Visible = True
		Me.mnuFileExit.MDIList = False
		Me.mnuProject.Text = "&Project"
		Me.mnuProject.Checked = False
		Me.mnuProject.Enabled = True
		Me.mnuProject.Visible = True
		Me.mnuProject.MDIList = False
		Me.mnuProjectBrowse.Text = "&Browse"
		Me.mnuProjectBrowse.Checked = False
		Me.mnuProjectBrowse.Enabled = True
		Me.mnuProjectBrowse.Visible = True
		Me.mnuProjectBrowse.MDIList = False
		Me.mnuProjectBrowseGame.Text = "&Game..."
		Me.mnuProjectBrowseGame.Checked = False
		Me.mnuProjectBrowseGame.Enabled = True
		Me.mnuProjectBrowseGame.Visible = True
		Me.mnuProjectBrowseGame.MDIList = False
		Me.mnuProjectBrowseTrainer.Text = "&Trainer..."
		Me.mnuProjectBrowseTrainer.Checked = False
		Me.mnuProjectBrowseTrainer.Enabled = True
		Me.mnuProjectBrowseTrainer.Visible = True
		Me.mnuProjectBrowseTrainer.MDIList = False
		Me.mnuProjectDash1.Text = "-"
		Me.mnuProjectDash1.Checked = False
		Me.mnuProjectDash1.Enabled = True
		Me.mnuProjectDash1.Visible = True
		Me.mnuProjectDash1.MDIList = False
		Me.mnuProjectSwitch.Text = "&Switch"
		Me.mnuProjectSwitch.Checked = False
		Me.mnuProjectSwitch.Enabled = True
		Me.mnuProjectSwitch.Visible = True
		Me.mnuProjectSwitch.MDIList = False
		Me.mnuProjrectSwitchGame.Text = "&Game..."
		Me.mnuProjrectSwitchGame.Checked = False
		Me.mnuProjrectSwitchGame.Enabled = True
		Me.mnuProjrectSwitchGame.Visible = True
		Me.mnuProjrectSwitchGame.MDIList = False
		Me.mnuProjectSwitchTrainer.Text = "&Trainer..."
		Me.mnuProjectSwitchTrainer.Checked = False
		Me.mnuProjectSwitchTrainer.Enabled = True
		Me.mnuProjectSwitchTrainer.Visible = True
		Me.mnuProjectSwitchTrainer.MDIList = False
		Me.mnuProjectDash2.Text = "-"
		Me.mnuProjectDash2.Checked = False
		Me.mnuProjectDash2.Enabled = True
		Me.mnuProjectDash2.Visible = True
		Me.mnuProjectDash2.MDIList = False
		Me.mnuProjectLaunch.Text = "&Launch"
		Me.mnuProjectLaunch.Shortcut = System.Windows.Forms.Shortcut.CtrlL
		Me.mnuProjectLaunch.Checked = False
		Me.mnuProjectLaunch.Enabled = True
		Me.mnuProjectLaunch.Visible = True
		Me.mnuProjectLaunch.MDIList = False
		Me.mnuHelp.Text = "&Help"
		Me.mnuHelp.Checked = False
		Me.mnuHelp.Enabled = True
		Me.mnuHelp.Visible = True
		Me.mnuHelp.MDIList = False
		Me.mnuHelpAbout.Text = "&About..."
		Me.mnuHelpAbout.Checked = False
		Me.mnuHelpAbout.Enabled = True
		Me.mnuHelpAbout.Visible = True
		Me.mnuHelpAbout.MDIList = False
		Me.mnuHelpCheckForUpdate.Text = "&Check for Update"
		Me.mnuHelpCheckForUpdate.Checked = False
		Me.mnuHelpCheckForUpdate.Enabled = True
		Me.mnuHelpCheckForUpdate.Visible = True
		Me.mnuHelpCheckForUpdate.MDIList = False
		Me.Controls.Add(cmdLaunch)
		Me.Controls.Add(fraTrainerData)
		Me.Controls.Add(fraGameData)
		Me.Controls.Add(fraStoreFileDetails)
		Me.Controls.Add(cdgStoreFile)
		Me.fraTrainerData.Controls.Add(txtTrainerPath)
		Me.fraTrainerData.Controls.Add(picXPShim3)
		Me.picXPShim3.Controls.Add(cmdBrowseTrainer)
		Me.picXPShim3.Controls.Add(txtTrainerSwitch)
		Me.picXPShim3.Controls.Add(lblTrainerSwitch)
		Me.fraGameData.Controls.Add(picXPShim2)
		Me.picXPShim2.Controls.Add(cmdBrowseGame)
		Me.picXPShim2.Controls.Add(txtGameSwitch)
		Me.picXPShim2.Controls.Add(txtGamePath)
		Me.picXPShim2.Controls.Add(lblGameSwitch)
		Me.fraStoreFileDetails.Controls.Add(picXPShim1)
		Me.picXPShim1.Controls.Add(chkLaunchGameFirst)
		Me.picXPShim1.Controls.Add(chkTrainerLaunchesGame)
		CType(Me.cdgStoreFile, System.ComponentModel.ISupportInitialize).EndInit()
		Me.mnuFile.Index = 0
		Me.mnuProject.Index = 1
		Me.mnuHelp.Index = 2
		MainMenu1.MenuItems.AddRange(New System.Windows.Forms.MenuItem(){Me.mnuFile, Me.mnuProject, Me.mnuHelp})
		Me.mnuFileNew.Index = 0
		Me.mnuFileOpen.Index = 1
		Me.mnuFileDash1.Index = 2
		Me.mnuFileSave.Index = 3
		Me.mnuFileSaveAs.Index = 4
		Me.mnuFileDash2.Index = 5
		Me.mnuFileExit.Index = 6
		mnuFile.MenuItems.AddRange(New System.Windows.Forms.MenuItem(){Me.mnuFileNew, Me.mnuFileOpen, Me.mnuFileDash1, Me.mnuFileSave, Me.mnuFileSaveAs, Me.mnuFileDash2, Me.mnuFileExit})
		Me.mnuProjectBrowse.Index = 0
		Me.mnuProjectDash1.Index = 1
		Me.mnuProjectSwitch.Index = 2
		Me.mnuProjectDash2.Index = 3
		Me.mnuProjectLaunch.Index = 4
		mnuProject.MenuItems.AddRange(New System.Windows.Forms.MenuItem(){Me.mnuProjectBrowse, Me.mnuProjectDash1, Me.mnuProjectSwitch, Me.mnuProjectDash2, Me.mnuProjectLaunch})
		Me.mnuProjectBrowseGame.Index = 0
		Me.mnuProjectBrowseTrainer.Index = 1
		mnuProjectBrowse.MenuItems.AddRange(New System.Windows.Forms.MenuItem(){Me.mnuProjectBrowseGame, Me.mnuProjectBrowseTrainer})
		Me.mnuProjrectSwitchGame.Index = 0
		Me.mnuProjectSwitchTrainer.Index = 1
		mnuProjectSwitch.MenuItems.AddRange(New System.Windows.Forms.MenuItem(){Me.mnuProjrectSwitchGame, Me.mnuProjectSwitchTrainer})
		Me.mnuHelpAbout.Index = 0
		Me.mnuHelpCheckForUpdate.Index = 1
		mnuHelp.MenuItems.AddRange(New System.Windows.Forms.MenuItem(){Me.mnuHelpAbout, Me.mnuHelpCheckForUpdate})
		Me.Menu = MainMenu1
	End Sub
#End Region 
#Region "Upgrade Support "
	Private Shared m_vb6FormDefInstance As frmMain
	Private Shared m_InitializingDefInstance As Boolean
	Public Shared Property DefInstance() As frmMain
		Get
			If m_vb6FormDefInstance Is Nothing OrElse m_vb6FormDefInstance.IsDisposed Then
				m_InitializingDefInstance = True
				m_vb6FormDefInstance = New frmMain()
				m_InitializingDefInstance = False
			End If
			DefInstance = m_vb6FormDefInstance
		End Get
		Set
			m_vb6FormDefInstance = Value
		End Set
	End Property
#End Region 
	
	
	Private Sub frmMain_Load(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Load
		SetFileName("untitled")
		SetFileFlag(False, Me)
		If DoesFileExist(VB6.GetPath & "\update.exe") = True Then
			Me.mnuHelpCheckForUpdate.Visible = True
		End If
		If DoesFileExist(VB6.GetPath & "\update.exe") = False Then
			Me.mnuHelpCheckForUpdate.Visible = False
		End If
	End Sub
	
	
	
	'UPGRADE_WARNING: Form event frmMain.QueryUnload has a new behavior. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup2065"'
	Private Sub frmMain_Closing(ByVal eventSender As System.Object, ByVal eventArgs As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
		Dim Cancel As Short = eventArgs.Cancel
		Dim iMSG As MsgBoxResult
		
		' When a StoreFile is launched, we must quickly close up
		If GetLaunchingSTF = True Then
			Cancel = False
			GoTo EventExitSub
		End If
		
		' A files is not being launched
		Dim strSaveFile As String
		If GetFileFlag() = False Then
			iMSG = MsgBox("Are you sure you want to exit " & System.Reflection.Assembly.GetExecutingAssembly.GetName.Name & "?", MsgBoxStyle.Question + MsgBoxStyle.YesNo)
			If iMSG = MsgBoxResult.No Then
				Cancel = True
			End If
			If iMSG = MsgBoxResult.Yes Then
				Cancel = False
			End If
		Else
			
			iMSG = MsgBox("Would you like to save your work before exiting?", MsgBoxStyle.Question + MsgBoxStyle.YesNoCancel, System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName)
			If iMSG = MsgBoxResult.Yes Then
				' File isn't saved yet.  Show a Save As dialog
				If LCase(GetFileName()) = "untitled" Then
					strSaveFile = DisplaySaveAs()
					If strSaveFile = "32755" Then
						' User clicked Cancel in the Save As dialog
						Cancel = True
						GoTo EventExitSub
					End If
					
					' Write that filename to memory
					SetFileName((strSaveFile))
				End If
				
				' Write data to disk
				SaveStoreFile(Me, GetFileName())
				Cancel = False
			End If
			If iMSG = MsgBoxResult.No Then
				Cancel = False
			End If
			If iMSG = MsgBoxResult.Cancel Then
				' Cancel.  No exit, No save.
				Cancel = True
			End If
		End If
EventExitSub: 
		eventArgs.Cancel = Cancel
	End Sub
	
	
	'UPGRADE_WARNING: Event chkLaunchGameFirst.CheckStateChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup2075"'
	Private Sub chkLaunchGameFirst_CheckStateChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles chkLaunchGameFirst.CheckStateChanged
		If chkLaunchGameFirst.CheckState = True Then
			SetLaunchGameFirst(True)
			GoTo SetFlag
		End If
		If chkLaunchGameFirst.CheckState = False Then
			SetLaunchGameFirst(False)
			GoTo SetFlag
		End If
SetFlag: 
		SetFileFlag(True, Me)
	End Sub
	
	'UPGRADE_WARNING: Event chkTrainerLaunchesGame.CheckStateChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup2075"'
	Private Sub chkTrainerLaunchesGame_CheckStateChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles chkTrainerLaunchesGame.CheckStateChanged
		' Turn on or off being able to locate a game if the trainer launches it
		Select Case chkTrainerLaunchesGame.CheckState
			Case System.Windows.Forms.CheckState.Checked
				SetTrainerLaunchesGame(True)
				Me.txtGamePath.Enabled = False
				Me.txtGameSwitch.Enabled = False
				Me.cmdBrowseGame.Enabled = False
				Me.mnuProjectBrowseGame.Enabled = False
				Me.mnuProjrectSwitchGame.Enabled = False
				Me.picXPShim2.Enabled = False
				Me.fraGameData.Enabled = False
				Me.lblGameSwitch.Enabled = False
				Me.txtGamePath.BackColor = System.Drawing.SystemColors.Control
				Me.txtGameSwitch.BackColor = System.Drawing.SystemColors.Control
				GoTo SetFlag
			Case System.Windows.Forms.CheckState.Unchecked
				SetTrainerLaunchesGame(False)
				Me.txtGamePath.Enabled = True
				Me.txtGameSwitch.Enabled = True
				Me.cmdBrowseGame.Enabled = True
				Me.mnuProjectBrowseGame.Enabled = True
				Me.mnuProjrectSwitchGame.Enabled = True
				Me.picXPShim2.Enabled = True
				Me.fraGameData.Enabled = True
				Me.lblGameSwitch.Enabled = True
				Me.txtGamePath.BackColor = System.Drawing.SystemColors.Window
				Me.txtGameSwitch.BackColor = System.Drawing.SystemColors.Window
				GoTo SetFlag
		End Select
SetFlag: 
		SetFileFlag(True, Me)
	End Sub
	
	Private Sub cmdBrowseGame_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles cmdBrowseGame.Click
		On Error GoTo Trap
		Dim winInfo As osInfo
		With Me.cdgStoreFile
			.CancelError = True
			.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
			.DefaultExt = "exe"
			.DialogTitle = "Locate Game"
			.Flags = MSComDlg.FileOpenConstants.cdlOFNFileMustExist
			.InitDir = Environ("ProgramFiles") ' Default to %PROGRAM_FILES% since most games would be there (unlike SpecialFolder, this works on Win95/98)
			.ShowOpen()
			Me.txtGamePath.Text = .FileName
			SetGamePath((.FileName))
		End With
		validateFile(txtGamePath)
		SetFileFlag(True, Me)
		Exit Sub
Trap: 
		'User clicked cancel
		Exit Sub
	End Sub
	
	Private Sub cmdBrowseTrainer_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles cmdBrowseTrainer.Click
		On Error GoTo Trap
		With Me.cdgStoreFile
			.CancelError = True
			.InitDir = FGetSpecialFolder(SpecialFolder.SpecialFolderID.CSIDL_DOCUMENTS) '%MY_DOCUMENTS%
			.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
			.DefaultExt = "exe"
			.DialogTitle = "Locate Trainer"
			.Flags = MSComDlg.FileOpenConstants.cdlOFNFileMustExist
			.ShowOpen()
			Me.txtTrainerPath.Text = .FileName
			SetTrainerPath((.FileName))
		End With
		validateFile(txtTrainerPath)
		SetFileFlag(True, Me)
		Exit Sub
Trap: 
		'User clicked cancel
		Exit Sub
	End Sub
	
	Private Sub cmdLaunch_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles cmdLaunch.Click
		doLaunch()
	End Sub
	
	Private Function DisplaySaveAs() As String
		On Error GoTo Trapper
		With Me.cdgStoreFile
			.CancelError = True
			.DialogTitle = "Save StoreFile"
			.Filter = "StoreFile (*.stf)|*.stf"
			.InitDir = GetStoreFileFolder()
			.DefaultExt = "stf"
			.ShowSave()
			DisplaySaveAs = .FileName
		End With
		Exit Function
Trapper: 
		If Err.Number = 32755 Then 'Cancel
			DisplaySaveAs = "32755"
			Exit Function
		End If
	End Function
	
	
	Public Sub mnuFileExit_Popup(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuFileExit.Popup
		mnuFileExit_Click(eventSender, eventArgs)
	End Sub
	Public Sub mnuFileExit_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuFileExit.Click
		Me.Close()
	End Sub
	
	Public Sub mnuFileNew_Popup(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuFileNew.Popup
		mnuFileNew_Click(eventSender, eventArgs)
	End Sub
	Public Sub mnuFileNew_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuFileNew.Click
		createNewStoreFile()
	End Sub
	
	Public Sub mnuFileOpen_Popup(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuFileOpen.Popup
		mnuFileOpen_Click(eventSender, eventArgs)
	End Sub
	Public Sub mnuFileOpen_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuFileOpen.Click
		On Error GoTo Trapper
		With Me.cdgStoreFile
			.CancelError = True
			.DialogTitle = "Open StoreFile"
			.DefaultExt = "stf"
			.Filter = "StoreTrainer File (*.stf)|*.stf"
			.InitDir = GetStoreFileFolder()
			.Flags = MSComDlg.FileOpenConstants.cdlOFNFileMustExist
			.ShowOpen()
			
			' Reads STF data into StoreTrainer
			OpenStoreFile(.FileName)
			
			' Brings StoreData into the form
			Me.chkLaunchGameFirst.CheckState = GetLaunchGameFirst
			Me.chkTrainerLaunchesGame.CheckState = GetTrainerLaunchesGame
			Me.txtGamePath.Text = GetGamePath
			Me.txtGameSwitch.Text = GetGameSwitch
			Me.txtTrainerPath.Text = GetTrainerPath
			Me.txtTrainerSwitch.Text = GetTrainerSwitch
			
			' Sets our form name
			SetFileName(.FileName)
			SetFileFlag(False, Me)
		End With
		Exit Sub
Trapper: 
		' Assume CancelError
		Exit Sub
	End Sub
	
	Public Sub mnuFileSave_Popup(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuFileSave.Popup
		mnuFileSave_Click(eventSender, eventArgs)
	End Sub
	Public Sub mnuFileSave_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuFileSave.Click
		' Bring up Save As if needed
		Dim strFileName As String
		strFileName = GetFileName()
		If CBool(LCase(CStr(strFileName = "untitled"))) Then
			mnuFileSaveAs_Click(mnuFileSaveAs, New System.EventArgs())
		End If
		
		' Write file to disk
		Dim iRet As Short
		iRet = SaveStoreFile(Me, GetFileName)
		
		' iRet is any error number the disk writing routine returns
		If iRet = 0 Then Exit Sub
		If iRet <> 0 Then
			Select Case iRet
				Case 32755 'Cancel Button somewhere
					Exit Sub
				Case Else
					MsgBox("Error " & iRet)
			End Select
		End If
	End Sub
	
	Public Sub mnuFileSaveAs_Popup(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuFileSaveAs.Popup
		mnuFileSaveAs_Click(eventSender, eventArgs)
	End Sub
	Public Sub mnuFileSaveAs_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuFileSaveAs.Click
		' Brings up a Save As dialog
		Dim strFile As String
		strFile = DisplaySaveAs()
		If strFile = "32755" Then
			Exit Sub
		End If
		
		' Continues with writing file to disk here
		SetFileName((strFile))
		mnuFileSave_Click(mnuFileSave, New System.EventArgs())
		
	End Sub
	
	Public Sub mnuHelpAbout_Popup(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuHelpAbout.Popup
		mnuHelpAbout_Click(eventSender, eventArgs)
	End Sub
	Public Sub mnuHelpAbout_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuHelpAbout.Click
		AboutStoreTrainer()
	End Sub
	
	Public Sub mnuHelpCheckForUpdate_Popup(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuHelpCheckForUpdate.Popup
		mnuHelpCheckForUpdate_Click(eventSender, eventArgs)
	End Sub
	Public Sub mnuHelpCheckForUpdate_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuHelpCheckForUpdate.Click
		If DoesFileExist(VB6.GetPath & "\update.exe") = False Then
			MsgBox("The updater is missing!", MsgBoxStyle.Exclamation + MsgBoxStyle.OKOnly, System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName)
			Exit Sub
		End If
		Shell(VB6.GetPath & "\update.exe", AppWinStyle.NormalFocus)
	End Sub
	
	Public Sub mnuProjectBrowseGame_Popup(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuProjectBrowseGame.Popup
		mnuProjectBrowseGame_Click(eventSender, eventArgs)
	End Sub
	Public Sub mnuProjectBrowseGame_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuProjectBrowseGame.Click
		cmdBrowseGame_Click(cmdBrowseGame, New System.EventArgs())
	End Sub
	
	Public Sub mnuProjectBrowseTrainer_Popup(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuProjectBrowseTrainer.Popup
		mnuProjectBrowseTrainer_Click(eventSender, eventArgs)
	End Sub
	Public Sub mnuProjectBrowseTrainer_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuProjectBrowseTrainer.Click
		cmdBrowseTrainer_Click(cmdBrowseTrainer, New System.EventArgs())
	End Sub
	
	Public Sub mnuProjectLaunch_Popup(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuProjectLaunch.Popup
		mnuProjectLaunch_Click(eventSender, eventArgs)
	End Sub
	Public Sub mnuProjectLaunch_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuProjectLaunch.Click
		doLaunch()
	End Sub
	
	Public Sub mnuProjectSwitchTrainer_Popup(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuProjectSwitchTrainer.Popup
		mnuProjectSwitchTrainer_Click(eventSender, eventArgs)
	End Sub
	Public Sub mnuProjectSwitchTrainer_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuProjectSwitchTrainer.Click
		On Error GoTo Done 'We don't need to handle any errors
		Dim strTrainerSwitch As String
		strTrainerSwitch = InputBox("Please enter a trainer switch", "Input Trainer Switch")
		If strTrainerSwitch <> "" Then
			txtTrainerSwitch.Text = strTrainerSwitch
			SetTrainerSwitch((strTrainerSwitch))
			SetFileFlag(True, Me)
		End If
Done: 
		Exit Sub
	End Sub
	
	Public Sub mnuProjrectSwitchGame_Popup(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuProjrectSwitchGame.Popup
		mnuProjrectSwitchGame_Click(eventSender, eventArgs)
	End Sub
	Public Sub mnuProjrectSwitchGame_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuProjrectSwitchGame.Click
		On Error GoTo Done 'We don't need to handle any errors
		Dim strGameSwitch As String
		strGameSwitch = InputBox("Please enter a game switch", "Input Game Switch")
		If strGameSwitch <> "" Then
			txtGameSwitch.Text = strGameSwitch
			SetGameSwitch((strGameSwitch))
			SetFileFlag(True, Me)
		End If
Done: 
		Exit Sub
	End Sub
	
	'UPGRADE_WARNING: Event txtGamePath.TextChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup2075"'
	Private Sub txtGamePath_TextChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles txtGamePath.TextChanged
		SetFileFlag(True, Me)
	End Sub
	
	Private Sub txtGamePath_Enter(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles txtGamePath.Enter
		validateFile(txtGamePath)
	End Sub
	
	Private Sub txtGamePath_Leave(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles txtGamePath.Leave
		validateFile(txtGamePath)
	End Sub
	
	'UPGRADE_WARNING: Event txtGameSwitch.TextChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup2075"'
	Private Sub txtGameSwitch_TextChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles txtGameSwitch.TextChanged
		SetGameSwitch((txtGameSwitch.Text))
		SetFileFlag(True, Me)
	End Sub
	
	'UPGRADE_WARNING: Event txtTrainerPath.TextChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup2075"'
	Private Sub txtTrainerPath_TextChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles txtTrainerPath.TextChanged
		SetFileFlag(True, Me)
	End Sub
	
	Private Sub txtTrainerPath_Enter(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles txtTrainerPath.Enter
		validateFile(txtTrainerPath)
	End Sub
	
	Private Sub txtTrainerPath_Leave(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles txtTrainerPath.Leave
		validateFile(txtTrainerPath)
	End Sub
	
	'UPGRADE_WARNING: Event txtTrainerSwitch.TextChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup2075"'
	Private Sub txtTrainerSwitch_TextChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles txtTrainerSwitch.TextChanged
		SetTrainerSwitch((txtTrainerSwitch.Text))
		SetFileFlag(True, Me)
	End Sub
	
	Private Sub validateFile(ByRef txt As System.Windows.Forms.TextBox)
		If txt.Text = "" Then Exit Sub
		
		If DoesFileExist((txt.Text)) = False Then
			txt.BackColor = System.Drawing.Color.Red
			MsgBox("The file " & Chr(34) & txtGamePath.Text & Chr(34) & " does not exist!", MsgBoxStyle.Exclamation, System.Reflection.Assembly.GetExecutingAssembly.GetName.Name)
		Else
			txt.BackColor = System.Drawing.SystemColors.Window
		End If
	End Sub
	Private Sub createNewStoreFile()
		Dim iMSG As MsgBoxResult
		If GetFileFlag = True Then
			' Save first?
			iMSG = MsgBox("Would you like to save your work before creating a new StoreFile?", MsgBoxStyle.YesNoCancel + MsgBoxStyle.Question, System.Reflection.Assembly.GetExecutingAssembly.GetName.Name)
			If iMSG = MsgBoxResult.Cancel Then Exit Sub
			If iMSG = MsgBoxResult.Yes Then
				mnuFileSave_Click(mnuFileSave, New System.EventArgs())
				GoTo CreateNewFile
			End If
			If iMSG = MsgBoxResult.No Then GoTo CreateNewFile
		End If
		
		If GetFileFlag = False Then
			iMSG = MsgBox("Close " & GetFileName(True) & " and create a new StoreFile?", MsgBoxStyle.YesNo + MsgBoxStyle.Question, System.Reflection.Assembly.GetExecutingAssembly.GetName.Name)
			If iMSG = MsgBoxResult.No Then Exit Sub
			If iMSG = MsgBoxResult.Yes Then GoTo CreateNewFile
		End If
		
		' Done the formalities.  Create our new StoreFile
CreateNewFile: 
		NewStoreFile()
		Me.chkLaunchGameFirst.CheckState = GetLaunchGameFirst
		Me.chkTrainerLaunchesGame.CheckState = GetTrainerLaunchesGame
		Me.txtGamePath.Text = GetGamePath
		Me.txtGameSwitch.Text = GetGameSwitch
		Me.txtTrainerPath.Text = GetTrainerPath
		Me.txtTrainerSwitch.Text = GetTrainerSwitch
		SetFileFlag(False, Me)
	End Sub
	
	Private Sub doLaunch()
		Dim iLaunch As Short
		iLaunch = Launch()
		If iLaunch <> 0 Then
			Select Case iLaunch
				Case 1 'File not saved
					MsgBox("Please save your work before launching the StoreFile!", MsgBoxStyle.Exclamation, System.Reflection.Assembly.GetExecutingAssembly.GetName.Name)
					Exit Sub
				Case 2 ' Bad game path
					MsgBox("The game path you have doesn't exist!", MsgBoxStyle.Exclamation, System.Reflection.Assembly.GetExecutingAssembly.GetName.Name)
					Exit Sub
				Case 3 'Bad trainer path
					MsgBox("The trainer path you have doesn't exist!", MsgBoxStyle.Exclamation, System.Reflection.Assembly.GetExecutingAssembly.GetName.Name)
					Exit Sub
			End Select
		End If
	End Sub
End Class