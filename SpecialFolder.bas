Attribute VB_Name = "SpecialFolder"
Option Explicit


' Gives us access to Special Folders
Declare Function SHGetSpecialFolderLocation Lib "Shell32.dll" _
(ByVal hwndOwner As Long, ByVal nFolder As Long, pidl As ITEMIDLIST) As Long
Declare Function SHGetPathFromIDList Lib "Shell32.dll" Alias "SHGetPathFromIDListA" _
(ByVal pidl As Long, ByVal pszPath As String) As Long

Public Type SHITEMID
    cb As Long
    abID As Byte
End Type

Public Type ITEMIDLIST
    mkid As SHITEMID
End Type
Public Const MAX_PATH As Integer = 260

' List of folders
Public Enum SpecialFolderID
     CSIDL_DESKTOP = &H0 '// The Desktop - virtual folder
     CSIDL_APPDATA = &H1A '// Roaming App Data (anywhere on network)
     CSIDL_COMMONAPPDATA = &H23 '// Common App Data (probably do NOT have permission)
     CSIDL_LOCALAPPDATA = &H1C '// Same as App Data, but stays LOCAL to computer
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

Public Function FGetSpecialFolder(CSIDL As SpecialFolderID) As String
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
        sPath = Space$(MAX_PATH)
        If SHGetPathFromIDList(ByVal IDL.mkid.cb, ByVal sPath) Then
            FGetSpecialFolder = Left$(sPath, InStr(sPath, vbNullChar) - 1) & ""
        End If
    End If
End Function


Public Function GetStoreFileFolder(Optional doCreateFolder As Boolean = False, Optional doPromptCreateFolder As Boolean = False) As String
    Dim FSO As FileSystemObject
    Set FSO = CreateObject("Scripting.FileSystemObject")
    If DoesFolderExist(iniGetValueString(getStoreTrainerDataFile, "Library", "Folder", vbNullString)) Then
        ' My Documents\StoreFiles exists.  Give it to the user
        GoTo ReturnFolder
    Else
        ' The folder does not exist
        If doCreateFolder Then
            ' The user wants to create the folder
            If doPromptCreateFolder Then
                ' The user needs a Yes/No
                Dim msgCreateFolder As VbMsgBoxResult
                msgCreateFolder = MsgBox("The StoreFile folder does not exist.  Create it?", vbYesNo + vbQuestion, App.ProductName)
                If (msgCreateFolder = vbYes) Then
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
    Dim errorLocation As Integer
    strMyDocuments = FGetSpecialFolder(CSIDL_DOCUMENTS)
    If DoesFolderExist(strMyDocuments & "\My StoreFiles") = False Then
        FSO.CreateFolder strMyDocuments & "\My StoreFiles"
    End If
    strTStoreLibrary = strMyDocuments & "\My StoreFiles"
    
    iniWriteValueString getStoreTrainerDataFile, "Library", "Folder", strTStoreLibrary
    MsgBox "StoreFile Library successfully created at: " & strTStoreLibrary, vbInformation, App.Title
    
    GoTo ReturnFolder
    
    ' Returns the folder path
ReturnFolder:
    GetStoreFileFolder = iniGetValueString(getStoreTrainerDataFile, "Library", "Folder", "NO_FOLDER_CREATED")
    Exit Function
    
NoFolderCreatedShowError:
    MsgBox Err.Number & " - " & Err.Description & vbCrLf & strMyDocuments
NoFolderCreated:
    
    ' Folder doesn't exist, user didn't create
    GetStoreFileFolder = "NO_FOLDER_CREATED"
                
End Function

' Does a folder exist?
Public Function DoesFolderExist(strFullPath As String) As Boolean
    Dim FSO As FileSystemObject
    Set FSO = CreateObject("Scripting.FileSystemObject")
    
    DoesFolderExist = FSO.FolderExists(strFullPath)
End Function

' Does a file exist?
Public Function DoesFileExist(strFullPath As String) As Boolean
    Dim FSO As FileSystemObject
    Set FSO = CreateObject("Scripting.FileSystemObject")
    
    DoesFileExist = FSO.FileExists(strFullPath)
End Function
