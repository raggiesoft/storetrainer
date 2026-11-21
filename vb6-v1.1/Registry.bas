Attribute VB_Name = "Registry"
Option Explicit

' ==============================================================================
' FILE: Registry.bas
' PROJECT: RaggieSoft StoreTrainer v1.1 (Classic Visual Basic 6.0 Edition)
' LICENSE: MIT License
'
' PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
'   Provides low-level Win32 'advapi32.dll' Registry helpers ('RegGetValue$' and
'   'RegSetValue') supporting both 'REG_SZ' strings and 'REG_DWORD' integers
'   across Windows 95 through Windows 11, ensuring 'RegCloseKey' always closes
'   the kernel handle after reading or writing.
' ==============================================================================

Type SECURITY_ATTRIBUTES
    nLength As Long
    lpSecurityDescriptor As Long
    bInheritHandle As Long
End Type

Public Declare Function RegCreateKeyEx& Lib "advapi32.dll" Alias "RegCreateKeyExA" (ByVal hKey&, ByVal lpSubKey$, ByVal Reserved&, ByVal lpClass$, ByVal dwOptions&, ByVal samDesired&, lpSecurityAttributes As SECURITY_ATTRIBUTES, phkResult&, lpdwDisposition&)
Public Declare Function RegSetValueEx& Lib "advapi32.dll" Alias "RegSetValueExA" (ByVal hKey&, ByVal lpszValueName$, ByVal dwRes&, ByVal dwType&, lpDataBuff As Any, ByVal nSize&)

Private Declare Function RegCloseKey& Lib "advapi32.dll" (ByVal hKey&)
Private Declare Function RegOpenKeyExA& Lib "advapi32.dll" (ByVal hKey&, ByVal lpszSubKey$, dwOptions&, ByVal samDesired&, lpHKey&)
Private Declare Function RegQueryValueExA& Lib "advapi32.dll" (ByVal hKey&, ByVal lpszValueName$, ByVal lpdwRes&, lpdwType&, ByVal lpDataBuff$, nSize&)
Private Declare Function RegQueryValueEx& Lib "advapi32.dll" Alias "RegQueryValueExA" (ByVal hKey&, ByVal lpszValueName$, ByVal lpdwRes&, lpdwType&, lpDataBuff&, nSize&)

Public Const HKEY_CLASSES_ROOT = &H80000000
Public Const HKEY_CURRENT_USER = &H80000001
Public Const HKEY_LOCAL_MACHINE = &H80000002
Public Const HKEY_USERS = &H80000003

Public Const ERROR_SUCCESS = 0&
Public Const REG_SZ = 1&
Public Const REG_DWORD = 4&

Public Const KEY_QUERY_VALUE = &H1&
Public Const KEY_SET_VALUE = &H2&
Public Const KEY_CREATE_SUB_KEY = &H4&
Public Const KEY_ENUMERATE_SUB_KEYS = &H8&
Public Const KEY_NOTIFY = &H10&
Public Const READ_CONTROL = &H20000
Public Const STANDARD_RIGHTS_READ = READ_CONTROL
Public Const STANDARD_RIGHTS_WRITE = READ_CONTROL
Public Const KEY_READ = STANDARD_RIGHTS_READ Or KEY_QUERY_VALUE Or KEY_ENUMERATE_SUB_KEYS Or KEY_NOTIFY
Public Const KEY_WRITE = STANDARD_RIGHTS_WRITE Or KEY_SET_VALUE Or KEY_CREATE_SUB_KEY

Public Function RegGetValue$(MainKey&, SubKey$, value$)
    Dim sKeyType&
    Dim ret&
    Dim lpHKey&
    Dim lpcbData&
    Dim ReturnedString$
    Dim ReturnedLong&
    If MainKey >= &H80000000 And MainKey <= &H80000006 Then
        ret = RegOpenKeyExA(MainKey, SubKey, 0&, KEY_READ, lpHKey)
        If ret <> ERROR_SUCCESS Then
            RegGetValue = ""
            Exit Function
        End If

        lpcbData = 255
        ReturnedString = Space$(lpcbData)
        ret& = RegQueryValueExA(lpHKey, value, ByVal 0&, sKeyType, ReturnedString, lpcbData)
        If ret <> ERROR_SUCCESS Then
            RegGetValue = ""
        Else
            If sKeyType = REG_DWORD Then
                ret = RegQueryValueEx(lpHKey, value, ByVal 0&, sKeyType, ReturnedLong, 4)
                If ret = ERROR_SUCCESS Then RegGetValue = CStr(ReturnedLong)
            ElseIf lpcbData > 0 Then
                RegGetValue = Left$(ReturnedString, lpcbData - 1)
            End If
        End If
        ret = RegCloseKey(lpHKey)
    End If
End Function

Public Sub RegSetValue(H_KEY&, RSubKey$, ValueName$, RegValue$)
    Dim lRtn&
    Dim hKey&
    Dim lpDisp&
    Dim Sec_Att As SECURITY_ATTRIBUTES
    Sec_Att.nLength = 12&
    Sec_Att.lpSecurityDescriptor = 0&
    Sec_Att.bInheritHandle = False
    If RegValue = "" Then RegValue = " "

    lRtn = RegCreateKeyEx(H_KEY, RSubKey, 0&, "", 0&, KEY_WRITE, Sec_Att, hKey, lpDisp)
    If lRtn <> 0 Then Exit Sub
    lRtn = RegSetValueEx(hKey, ValueName, 0&, REG_SZ, ByVal RegValue, CLng(Len(RegValue) + 1))
    lRtn = RegCloseKey(hKey)
End Sub
