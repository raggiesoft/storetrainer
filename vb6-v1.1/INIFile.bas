Attribute VB_Name = "INIFile"
Option Explicit

' ==============================================================================
' FILE: INIFile.bas
' PROJECT: RaggieSoft StoreTrainer v1.1 (Classic Visual Basic 6.0 Edition)
' LICENSE: MIT License
'
' PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
'   Wraps Windows' built-in 'GetPrivateProfileStringA' and 'WritePrivateProfileStringA'
'   functions inside 'kernel32.dll' so StoreTrainer can read and write '[Section]
'   Key=Value' lines in '.stf' StoreFiles and 'TStore.ini' on every Windows OS
'   from Windows 95 through Windows 11 with zero external database libraries!
'   Note: Buffer size was expanded from 256 to 1024 bytes in v1.1 so long nested
'   game directory paths and multi-flag command-line switches never truncate.
' ==============================================================================

Declare Function GetPrivateProfileString Lib "kernel32" Alias _
                 "GetPrivateProfileStringA" (ByVal lpApplicationName _
                 As String, ByVal lpKeyName As Any, ByVal lpDefault _
                 As String, ByVal lpReturnedString As String, ByVal _
                 nSize As Long, ByVal lpFileName As String) As Long

Declare Function WritePrivateProfileString Lib "kernel32" Alias _
                 "WritePrivateProfileStringA" (ByVal lpApplicationName _
                 As String, ByVal lpKeyName As Any, ByVal lpString As Any, _
                 ByVal lpFileName As String) As Long

Public Function IniGetValueString(sINIFile As String, sSection As String, sKey _
                As String, sDefault As String) As String
    Dim sTemp As String * 1024
    Dim nLength As Long
    sTemp = Space$(1024)
    nLength = GetPrivateProfileString(sSection, sKey, sDefault, sTemp, _
              1023, sINIFile)
    IniGetValueString = Left$(sTemp, nLength)
End Function

Public Sub IniWriteValueString(sINIFile As String, sSection As String, sKey _
           As String, sValue As String)
    Dim n As Long
    Dim sTemp As String
    sTemp = sValue
    ' Replace any CR/LF characters with spaces so a single value cannot inject extra INI lines
    For n = 1 To Len(sTemp)
        If Mid$(sTemp, n, 1) = vbCr Or Mid$(sTemp, n, 1) = vbLf Then
            Mid$(sTemp, n, 1) = " "
        End If
    Next n
    n = WritePrivateProfileString(sSection, sKey, sTemp, sINIFile)
End Sub
