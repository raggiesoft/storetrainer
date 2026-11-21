Attribute VB_Name = "SpecialFolder"
Option Explicit

' ==============================================================================
' FILE: SpecialFolder.bas
' PROJECT: RaggieSoft StoreTrainer v1.1 (Classic Visual Basic 6.0 Edition)
' LICENSE: MIT License
'
' PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
'   Queries 'Shell32.dll' ('SHGetSpecialFolderLocation' + 'SHGetPathFromIDListA')
'   using numeric CSIDL constants ('CSIDL_DOCUMENTS', 'CSIDL_APPDATA') so
'   StoreTrainer finds the user's real 'My Documents\My StoreFiles' and
'   'Application Data\RaggieSoft\StoreTrainer' folders on Windows 95/98/2000
'   just as reliably as on Windows 10 and Windows 11!
' ==============================================================================

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

Public Enum SpecialFolderID
    CSIDL_DESKTOP = &H0
    CSIDL_APPDATA = &H1A
    CSIDL_COMMONAPPDATA = &H23
    CSIDL_LOCALAPPDATA = &H1C
    CSIDL_PROGRAMS = 2
    CSIDL_CONTROLS = 3
    CSIDL_PRINTERS = 4
    CSIDL_DOCUMENTS = 5
    CSIDL_FAVORITES = 6
    CSIDL_STARTUP = 7
    CSIDL_RECENT = 8
    CSIDL_SENDTO = 9
    CSIDL_BITBUCKET = 10
    CSIDL_STARTMENU = 11
    CSIDL_DESKTOPFOLDER = 16
    CSIDL_DRIVES = 17
    CSIDL_NETWORK = 18
    CSIDL_NETHOOD = 19
    CSIDL_FONTS = 20
    CSIDL_SHELLNEW = 21
End Enum

Public Function FGetSpecialFolder(CSIDL As SpecialFolderID) As String
    Dim sPath As String
    Dim IDL As ITEMIDLIST
    FGetSpecialFolder = ""
    If SHGetSpecialFolderLocation(GetForegroundWindow(), CSIDL, IDL) = 0 Then
        sPath = Space$(MAX_PATH)
        If SHGetPathFromIDList(ByVal IDL.mkid.cb, ByVal sPath) Then
            FGetSpecialFolder = Left$(sPath, InStr(sPath, vbNullChar) - 1)
        End If
    End If
End Function

Public Function GetStoreFileFolder(Optional doCreateFolder As Boolean = False, Optional doPromptCreateFolder As Boolean = False) As String
    Dim FSO As FileSystemObject
    Set FSO = CreateObject("Scripting.FileSystemObject")

    Dim sConfigured As String
    sConfigured = IniGetValueString(GetStoreTrainerDataFile, "Library", "Folder", vbNullString)
    If sConfigured <> "" And DoesFolderExist(sConfigured) Then
        GetStoreFileFolder = sConfigured
        Exit Function
    End If

    If Not doCreateFolder Then
        GetStoreFileFolder = "NO_FOLDER_CREATED"
        Exit Function
    End If

    If doPromptCreateFolder Then
        Dim msgCreateFolder As VbMsgBoxResult
        msgCreateFolder = MsgBox("The StoreFile folder does not exist. Create it now in My Documents\My StoreFiles?", vbYesNo + vbQuestion, App.ProductName)
        If msgCreateFolder <> vbYes Then
            GetStoreFileFolder = "NO_FOLDER_CREATED"
            Exit Function
        End If
    End If

    On Error GoTo NoFolderCreatedShowError
    Dim strMyDocuments As String
    Dim strTStoreLibrary As String
    strMyDocuments = FGetSpecialFolder(CSIDL_DOCUMENTS)
    strTStoreLibrary = strMyDocuments & "\My StoreFiles"
    If DoesFolderExist(strTStoreLibrary) = False Then
        FSO.CreateFolder strTStoreLibrary
    End If

    IniWriteValueString GetStoreTrainerDataFile, "Library", "Folder", strTStoreLibrary
    GetStoreFileFolder = strTStoreLibrary
    Exit Function

NoFolderCreatedShowError:
    MsgBox Err.Number & " - " & Err.Description & vbCrLf & strMyDocuments, vbExclamation, App.ProductName
    GetStoreFileFolder = "NO_FOLDER_CREATED"
End Function

Public Function DoesFolderExist(strFullPath As String) As Boolean
    If Trim$(strFullPath) = "" Then
        DoesFolderExist = False
        Exit Function
    End If
    Dim FSO As FileSystemObject
    Set FSO = CreateObject("Scripting.FileSystemObject")
    DoesFolderExist = FSO.FolderExists(strFullPath)
End Function

Public Function DoesFileExist(strFullPath As String) As Boolean
    If Trim$(strFullPath) = "" Then
        DoesFileExist = False
        Exit Function
    End If
    Dim FSO As FileSystemObject
    Set FSO = CreateObject("Scripting.FileSystemObject")
    DoesFileExist = FSO.FileExists(strFullPath)
End Function
