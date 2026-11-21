Attribute VB_Name = "Registry"
Option Explicit

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
Public Const REG_SZ = 1&                          ' Unicode nul terminated string
Public Const REG_DWORD = 4&                       ' 32-bit number

Public Const KEY_QUERY_VALUE = &H1&
Public Const KEY_SET_VALUE = &H2&
Public Const KEY_CREATE_SUB_KEY = &H4&
Public Const KEY_ENUMERATE_SUB_KEYS = &H8&
Public Const KEY_NOTIFY = &H10&
Public Const KEY_CREATE_LINK = &H20&
Public Const READ_CONTROL = &H20000
Public Const WRITE_DAC = &H40000
Public Const WRITE_OWNER = &H80000
Public Const SYNCHRONIZE = &H100000
Public Const STANDARD_RIGHTS_REQUIRED = &HF0000
Public Const STANDARD_RIGHTS_READ = READ_CONTROL
Public Const STANDARD_RIGHTS_WRITE = READ_CONTROL
Public Const STANDARD_RIGHTS_EXECUTE = READ_CONTROL
Public Const KEY_READ = STANDARD_RIGHTS_READ Or KEY_QUERY_VALUE Or KEY_ENUMERATE_SUB_KEYS Or KEY_NOTIFY
Public Const KEY_WRITE = STANDARD_RIGHTS_WRITE Or KEY_SET_VALUE Or KEY_CREATE_SUB_KEY
Public Const KEY_EXECUTE = KEY_READ

Public Function RegGetValue$(MainKey&, SubKey$, value$)
   ' MainKey must be one of the Publicly declared HKEY constants.
   Dim sKeyType&       'to return the key type.  This function expects REG_SZ or REG_DWORD
   Dim ret&            'returned by registry functions, should be 0&
   Dim lpHKey&         'return handle to opened key
   Dim lpcbData&       'length of data in returned string
   Dim ReturnedString$ 'returned string value
   Dim ReturnedLong&   'returned long value
   If MainKey >= &H80000000 And MainKey <= &H80000006 Then
      ' Open key
      ret = RegOpenKeyExA(MainKey, SubKey, 0&, KEY_READ, lpHKey)
      If ret <> ERROR_SUCCESS Then
         RegGetValue = ""
         Exit Function     'No key open, so leave
      End If
      
      ' Set up buffer for data to be returned in.
      ' Adjust next value for larger buffers.
      lpcbData = 255
      ReturnedString = Space$(lpcbData)

      ' Read key
      ret& = RegQueryValueExA(lpHKey, value, ByVal 0&, sKeyType, ReturnedString, lpcbData)
      If ret <> ERROR_SUCCESS Then
         RegGetValue = ""   'Value probably doesn't exist
      Else
        If sKeyType = REG_DWORD Then
            ret = RegQueryValueEx(lpHKey, value, ByVal 0&, sKeyType, ReturnedLong, 4)
            If ret = ERROR_SUCCESS Then RegGetValue = CStr(ReturnedLong)
        Else
            RegGetValue = Left$(ReturnedString, lpcbData - 1)
        End If
    End If
      ' Always close opened keys.
      ret = RegCloseKey(lpHKey)
   End If
End Function

Public Sub RegSetValue(H_KEY&, RSubKey$, ValueName$, RegValue$)
    'H_KEY must be one of the Key Constants
    Dim lRtn&         'returned by registry functions, should be 0&
    Dim hKey&         'return handle to opened key
    Dim lpDisp&
    Dim Sec_Att As SECURITY_ATTRIBUTES
    Sec_Att.nLength = 12&
    Sec_Att.lpSecurityDescriptor = 0&
    Sec_Att.bInheritHandle = False
    If RegValue = "" Then RegValue = " "
    
        lRtn = RegCreateKeyEx(H_KEY, RSubKey, 0&, "", 0&, KEY_WRITE, Sec_Att, hKey, lpDisp)
        If lRtn <> 0 Then
            Exit Sub       'No key open, so leave
        End If
        lRtn = RegSetValueEx(hKey, ValueName, 0&, REG_SZ, ByVal RegValue, CLng(Len(RegValue) + 1))
        lRtn = RegCloseKey(hKey)
End Sub
